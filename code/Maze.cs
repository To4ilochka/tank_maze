using System;

public sealed class Maze : Component
{
	[Property] public CameraComponent CameraComponent { get; set; }
	[Property] public float CellSide { get; set; } = 2;
	[Property] public float WallThickness { get; set; } = 0.25f;
	[Property] public float WallHeight { get; set; } = 1f;
	[Property] public Color WallTint { get; set; } = "#575757";

	private readonly Random rnd = new();
	private Cell[][] maze;

	protected override void OnAwake()
	{
		int cellCountHeight = rnd.Int( 4, 5 ), cellCountWidth = rnd.Int( 6, 8 );
		CreateMaze( cellCountHeight, cellCountWidth );
		CameraComponent.OrthographicHeight = cellCountHeight * 100 + 200;
	}

	protected override void OnUpdate()
	{
		
	}

	public Vector3[] CreateSpawns( int count, int cellCountHeight, int cellCountWidth, int minDistance )
	{
		Vector3[] spawns = new Vector3[count];
		List<Cell> cells = new();

		foreach ( var array in maze )
		{
			foreach ( var cell in array )	
			{
				cells.Add( cell );
			}
		}

		for ( int c = 0; c < count; c++ )
		{
			Cell cell = rnd.FromList( cells );
			spawns[c] = cell.Transform.LocalPosition + new Vector3( CellSide * 25, CellSide * 25 );

			if ( c == count - 1 ) return spawns;

			for ( int i = Math.Max( 0, cell.Y - minDistance ); i <= Math.Min( cellCountHeight - 1, cell.Y + minDistance ); i++ )
			{
				for ( int j = Math.Max( 0, cell.X - minDistance ); j <= Math.Min( cellCountWidth - 1, cell.X + minDistance ); j++ )
				{
					cells.Remove( maze[i][j] );
				}
			}
		}

		return spawns;
	}

	public void CreateMaze( int cellCountHeight, int cellCountWidth )
	{
		float foundationHeight = cellCountHeight * CellSide, foundationWidth = cellCountWidth * CellSide;

		//		Creating a foundation
		CreateGameObj( "Foundation", Model.Plane, Color.White,
			Vector3.Zero,
			new( foundationHeight, foundationWidth, 0.01f ) );

		//		Creating a border walls
		CreateCube( "BorderWall",
			new( cellCountHeight * 50, 0, WallHeight * 25 ),
			new( WallThickness, foundationWidth + WallThickness, WallHeight ) );

		CreateCube( "BorderWall",
			new( cellCountHeight * -50, 0, WallHeight * 25 ),
			new( WallThickness, foundationWidth + WallThickness, WallHeight ) );

		CreateCube( "BorderWall",
			new( 0, cellCountWidth * 50, WallHeight * 25 ),
			new( foundationHeight + WallThickness, WallThickness, WallHeight ) );

		CreateCube( "BorderWall",
			new( 0, cellCountWidth * -50, WallHeight * 25 ),
			new( foundationHeight + WallThickness, WallThickness, WallHeight ) );

		//		Creating walls inside of maze(fill maze)
		FillMaze( cellCountHeight, cellCountWidth );
	}

	private void FillMaze( int cellCountHeight, int cellCountWidth )
	{
		int cellI = 1;
		var topLeftCorner = new Vector3( cellCountHeight * 50, cellCountWidth * 50, WallHeight * 25 );
		maze = new Cell[cellCountHeight][];
		maze[0] = new Cell[cellCountWidth];

		//	Init first column
		for ( int j = 0; j < cellCountWidth; j++ )
		{
			maze[0][j] = new( j, 0, cellI++, $"Cell({0};{j})", topLeftCorner + new Vector3( CellSide * -50, (j + 1) * CellSide * -50 ) )
			{
				Parent = GameObject
			};
		}

		for ( int i = 0; i < cellCountHeight; i++ )
		{
			if ( i > 0 )
			{
				maze[i] = new Cell[cellCountWidth];

				for ( int j = 0; j < cellCountWidth; j++ )
				{
					maze[i][j] = new( j, i, maze[i - 1][j].Value, $"Cell({i};{j})", topLeftCorner + new Vector3( (i + 1) * CellSide * -50, (j + 1) * CellSide * -50 ) )
					{
						Parent = GameObject
					};
					if ( maze[i - 1][j].DownWall != null ) maze[i][j].Value = cellI++;
				}
			}

			CreateRightWalls( cellCountWidth, i, i == cellCountHeight - 1 );
			if ( i != cellCountHeight - 1 ) CreateDownWalls( cellCountWidth, i );
		}
	}

	private void CreateRightWalls( int cellCountWidth, int i, bool isLastColonm )
	{
		for ( int j = 0; j < cellCountWidth - 1; j++ )
		{
			if ( maze[i][j].Value != maze[i][j + 1].Value )
			{
				if ( !isLastColonm )
				{
					if ( rnd.Int( 0, 1 ) == 1 )
					{
						maze[i][j].RightWall = CreateWall( maze[i][j], new Vector3( CellSide * 25, 0 ), false );
					}
					else
					{
						maze[i][j + 1].Value = maze[i][j].Value;
					}
				}
			}
			else
			{
				maze[i][j].RightWall = CreateWall( maze[i][j], new Vector3( CellSide * 25, 0 ), false );
			}
		}
	}

	private void CreateDownWalls( int cellCountWidth, int i )
	{
		for ( int j = 0; j < cellCountWidth; j++ )
		{
			if ( !IsUniqueValue( maze[i], maze[i][j].Value ) )
			{
				if ( rnd.Int( 0, 1 ) == 1 )
				{
					List<Cell> cellsWithoutDown = new();
					foreach ( var item in GetSameValues( maze[i], maze[i][j].Value ) )
					{
						if ( item.DownWall == null ) cellsWithoutDown.Add( item );
					}
					if ( cellsWithoutDown.Count >= 2 )
					{
						maze[i][j].DownWall = CreateWall( maze[i][j], new Vector3( 0, CellSide * 25 ), true );
					}
				}
			}
		}
	}

	private List<Cell> GetSameValues( Cell[] array, int value )
	{
		List<Cell> result = new();

		foreach ( var item in array )
		{
			if ( item.Value == value )
			{
				result.Add( item );
			}
		}
		return result;
	}

	private bool IsUniqueValue( Cell[] array, int value )
	{
		return GetSameValues( array, value ).Count == 1;
	}

	private GameObject CreateWall( GameObject parent, Vector3 localPosition, bool isWallHorisontal )
	{
		return CreateWall( "Wall", parent, localPosition, isWallHorisontal );
	}

	private GameObject CreateWall( string name, GameObject parent, Vector3 localPosition, bool isWallHorisontal )
	{
		return CreateCube
			(
			name,
			parent,
			localPosition,
			isWallHorisontal ? new( WallThickness, CellSide + WallThickness, WallHeight ) : new( CellSide + WallThickness, WallThickness, WallHeight )
			);
	}

	private GameObject CreateCube( string name, GameObject parent, Vector3 localPosition, Vector3 localScale )
	{
		var cube = CreateGameObj( name, parent, Model.Cube, WallTint, localPosition, localScale );
		cube.Components.Create<BoxCollider>();
		return cube;
	}

	private GameObject CreateCube( string name, Vector3 localPosition, Vector3 localScale )
	{
		var cube = CreateGameObj( name, Model.Cube, WallTint, localPosition, localScale );
		cube.Components.Create<BoxCollider>();
		return cube;
	}

	private GameObject CreateGameObj( string name, GameObject parent, Model model, Color tint, Vector3 localPosition, Vector3 localScale )
	{
		GameObject obj = new( true, name )
		{
			Parent = parent
		};

		obj.Transform.LocalPosition += localPosition;
		obj.Transform.LocalScale = localScale;

		var modelRenderer = obj.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.Tint = tint;

		return obj;
	}

	private GameObject CreateGameObj( string name, Model model, Color tint, Vector3 localPosition, Vector3 localScale )
	{
		GameObject obj = new( true, name )
		{
			Parent = GameObject
		};

		obj.Transform.LocalPosition += localPosition;
		obj.Transform.LocalScale = localScale;

		var modelRenderer = obj.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.Tint = tint;

		return obj;
	}

	private class Cell : GameObject
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Value { get; set; } = 0;
		public GameObject RightWall { get; set; }
		public GameObject DownWall { get; set; }

		public Cell( int x, int y, int value, string name, Vector3 localPosition ) : base( true, name )
		{
			X = x;
			Y = y;
			Value = value;
			Transform.LocalPosition = localPosition;
		}
	}
}

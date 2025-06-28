using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bytenol.Tetris.Scripts;


using Matrix_t = List<List<int>>;

delegate bool RotateCbDel(Matrix_t m);

struct TetroNext
{
	public Matrix_t data;
	public int Sprite;
}


public partial class Tetromino : Node2D
{
	private static TetroNext CurrentTetromino;
	private static readonly List<TetroNext> nextTetromino;
	private static readonly Random random;
	public static readonly Texture2D BlockTexture;

	private Vector2I pos, shadowTrans;
	private Timer timer, saveTimer;

	private delegate void OnLoopDel(int i, int j);
	public delegate void OnSaveDel(Tetromino tetromino);

	public static event OnSaveDel OnSave;


	static Tetromino()
	{
		random = new Random();
		nextTetromino = new();
		AddNextTetromino(4);
		BlockTexture = (Texture2D)ResourceLoader.Load("res://assets/tileset.png");

		Tetris.OnRestart += () =>
		{
			nextTetromino.Clear();
		};
	}


	private static void AddNextTetromino(int amount)
	{
		for(int i = 0; i < amount; i++) 
		{
			int id = random.Next(0, gridData.Count);
			int sprite = random.Next(0, 7);
			var data = gridData[id];
			int amountToRotate = random.Next(0, 10);
			for(int j = 0; j < amountToRotate; j++)
				Rotate(ref data, (rotatedPiece) => true);
			nextTetromino.Add(new() {
				data = data,
				Sprite = sprite
			});
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		timer = GetNode<Timer>("Timer");
		saveTimer = GetNode<Timer>("SaveTimer");
		ReSpawn();
		timer.Timeout += () => { pos.Y++; };
		saveTimer.WaitTime = 3.0f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(!(Tetris.PlayState == GameState.PLAYING)) 
			return;

		if (shadowTrans.Y < 0)
			Tetris.PlayState = GameState.OVER;

		Move();
		if(pos.Y == shadowTrans.Y) Save();
		QueueRedraw();
	}

	public override void _Draw()
	{
		var size = GridDrawer.Size;

		shadowTrans = pos;
		while(!IsColliding(shadowTrans, CurrentTetromino.data))
			shadowTrans.Y++;

		shadowTrans.Y--;

		OnDataLoop((i, j) => {
			var p = GridDrawer.GetPosition(pos.Y + i, pos.X + j);
			Rect2 dst = new(p.X, p.Y, size, size);
			Rect2 src = new(CurrentTetromino.Sprite * 30, 0, 30, 30);
			DrawTextureRectRegion(BlockTexture, dst, src);

			// render shadow
			var shadowPos = GridDrawer.GetPosition(shadowTrans.Y + i, shadowTrans.X + j);
			Rect2 rect = new(shadowPos.X, shadowPos.Y, size, size);
			DrawRect(rect, Colors.Red, false, 2.0f);
		}, ref CurrentTetromino.data);

		for(int i = 0; i < nextTetromino.Count; i++)
		{
			var nextTet = nextTetromino[i];
			float sz = 13;
			float wsz = sz * 4;
			Vector2 pos = new(325, 200 + i * wsz * 1.2f);
			OnDataLoop((i, j) => {
				Vector2 p = pos + new Vector2(j * sz, i * sz);
				Rect2 dst = new(p.X, p.Y, sz * 0.9f, sz * 0.9f);
				Rect2 src = new(nextTet.Sprite * 30, 0, sz, sz);
				DrawTextureRectRegion(BlockTexture, dst, src);
			}, ref nextTet.data);
			Rect2 rect = new(pos.X, pos.Y, wsz, wsz);
			DrawRect(rect, Colors.Black, false, 1.0f);
		}

	}



	// Called every time the tetromino is due for reset. After 'saved' or when game starts
	private void ReSpawn()
	{
		CurrentTetromino = nextTetromino[0];
		nextTetromino.RemoveAt(0);
		AddNextTetromino(1);
		pos = new(random.Next(0, GridDrawer.Width - CurrentTetromino.data[0].Count), -CurrentTetromino.data.Count - 1);
		shadowTrans = pos + new Vector2I(0, GridDrawer.Height);
		timer.WaitTime = Math.Clamp(Math.Abs(0.85f - Tetris.GetLevel() / 20.0f), 0.1f, 0.85f);
	}

	private void Move()
	{
		Vector2I vel = new(0, 0);

		if(Input.IsActionJustPressed("arrowLeft"))
			vel = Vector2I.Left;
		else if(Input.IsActionJustPressed("arrowRight"))
			vel = Vector2I.Right;
		else if(Input.IsActionJustPressed("arrowDown"))
			vel = Vector2I.Down;
		else if(Input.IsActionJustPressed("arrowUp"))
			RotatePiece();
			

		var oldPos = pos;
		var newPos = pos + vel;
		pos = IsColliding(newPos, CurrentTetromino.data) ? oldPos: newPos;
	}


	private static void Rotate(ref Matrix_t oldMatrix, RotateCbDel cb)
	{
		Matrix_t oldPiece = oldMatrix.Select(row => row.Select(num => num).ToList()).ToList();
		Matrix_t rotatedPiece = new();

		// transpose of a matrix
		for(int i = 0; i < oldPiece[0].Count; i++)
		{
			rotatedPiece.Add(new());
			for(int j = 0; j < oldPiece.Count; j++)
				rotatedPiece[i].Add(oldPiece[oldPiece.Count - 1 - j][i]);
		}

		if(cb(rotatedPiece))
			oldMatrix = rotatedPiece;
	}

	private void RotatePiece()
	{
		Rotate(ref CurrentTetromino.data, (rotatedPiece) => {
			return !IsOutOfBound(pos, ref rotatedPiece);
		});
			
	}

	private static bool IsColliding(Vector2I translation, Matrix_t matrix)
	{
		var boundary = GetBoundary(translation, ref matrix);
		if(boundary.Size.Y >= GridDrawer.Height || boundary.Size.X >= GridDrawer.Width
			|| boundary.Position.X < 0)
			return true;

		bool res = false;
		OnDataLoop((i, j) => {
			int row = translation.Y + i;
			int col = translation.X + j;
			if(row >= 0) {
				int gridId = GridDrawer.GetBoardAt(row, col);
				if(gridId != GridDrawer.EmptyVal) res = true;
			}
		}, ref matrix);

		return res;
	}

	// Check if a tetromino is out of the grid rectangular region
	// Returns true or false if otherwise
	private static bool IsOutOfBound(Vector2I pos, ref Matrix_t matrix)
	{
		var boundary = GetBoundary(pos, ref matrix);
		var p = boundary.Position;
		var s = boundary.Size;

		return p.X < 0 || p.Y < 0 || s.X >= GridDrawer.Width || s.Y >= GridDrawer.Height;
	}

	// This method saves the data of the current tetromino to the game's board array
	private void Save()
	{
		OnDataLoop((i, j) => {
			Vector2I p = new(pos.Y + i, pos.X + j);
			GridDrawer.SetBoardAt(p.X, p.Y, CurrentTetromino.Sprite);
		}, ref CurrentTetromino.data);

		OnSave(this);

		ReSpawn();
	}


	// Get the rectangular boundary for the tetromino silohuette
	// matrix: is the 2d array that defines the silohuette of the tetromino
	// pos: is the position of the matrix starting from the top-left
	private static Rect2 GetBoundary(Vector2I pos, ref Matrix_t matrix)
	{
		int minX = matrix[0].Count, maxX = 0, minY = matrix.Count, maxY = 0;

		OnDataLoop((i, j) => {
			minX = Math.Min(minX, j);
			maxX = Math.Max(maxX, j);
			minY = Math.Min(minY, i);
			maxY = Math.Max(maxY, i);
		}, ref matrix);

		return new(pos.X + minX, pos.Y + minY, pos.X + maxX, pos.Y + maxY);
	}


	private static void OnDataLoop(OnLoopDel cb, ref Matrix_t matrix)
	{
		for(int i = 0; i < matrix.Count; i++)
			for(int j = 0; j < matrix[0].Count; j++)
				if(matrix[i][j] != 0)
					cb(i, j);
	}


	private readonly static List<Matrix_t>  gridData = new() 
	{
		new()
		{
			new List<int>{1,1,0},
			new List<int>{0,1,1}
		},

		new()
		{
			new List<int>{0,1,1},
			new List<int>{1,1,0}
		},

		new()
		{
			new List<int>{1,1,0},
			new List<int>{0,1,1}
		},

		new()
		{
			new List<int>{0,0,1},
			new List<int>{0,0,1},
			new List<int>{0,1,1},
		},

		new()
		{
			new List<int>{1,0,0},
			new List<int>{1,0,0},
			new List<int>{1,1,0},
		},

		new()
		{
			new List<int>{0,0,0,0},
			new List<int>{1,1,1,1},
			new List<int>{0,0,0,0},
		},

		new()
		{
			new List<int>{1,1,1},
			new List<int>{0,1,0}
		},

		new()
		{
			new List<int>{1,1},
			new List<int>{1,1}
		}
	};

}
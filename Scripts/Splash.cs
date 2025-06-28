using Godot;
using System;

public partial class Splash : Control
{
	private static Timer SplashTimer;
	private static TextureRect SplashIcon;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SplashTimer = GetNode<Timer>("Timer");
		SplashIcon = GetNode<TextureRect>("TextureRect");

		var tween = GetTree().CreateTween();
		tween.TweenProperty(SplashIcon, "scale", new Vector2(0, 0), 6);

		SplashTimer.Timeout += () =>
		{
			GetTree().ChangeSceneToFile("res://scenes/play.tscn");
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

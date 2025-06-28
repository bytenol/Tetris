using Godot;

namespace Bytenol.Tetris.Scripts;

// delegate method for a function that returns void
public delegate void VoidCallbackFn();

public delegate void GameStateDel();

public enum GameState
{
    INIT,
    PLAYING,
    OVER,
}


public partial class Tetris : Control
{

    private static Label scoreLabel, levelLabel;
        
    public static event GameStateDel OnGameState;

    public static int Score { get; private set; } = 0;
    public static GameState PlayState;


    private static ColorRect gameOverScreen;
    private static Button RestartBtn;

    public static event VoidCallbackFn OnRestart;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        scoreLabel = GetNode<Label>("UI/Info/Score");
        levelLabel = GetNode<Label>("UI/Info/Level");
        gameOverScreen = GetNode<ColorRect>("UI/GameOverScreen");
        RestartBtn = GetNode<Button>("UI/GameOverScreen/Button");

        //if(GridDrawer.OnClear)

        GridDrawer.OnClear += OnGridClear;
        PlayState = GameState.PLAYING;

        RestartBtn.Connect("pressed", new Callable(this, nameof(RestartGame)));
    }

    private void RestartGame()
    {
        Score = 0;
        OnRestart();
        PlayState = GameState.PLAYING;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        gameOverScreen.Visible = PlayState == GameState.OVER;
    }

    public static int GetLevel()
    {
        return (Score / 20) + 1;
    }

    private static void OnGridClear(int clearCount)
    {
        Score += clearCount * 2; 
        scoreLabel.Text = Score.ToString();
        levelLabel.Text = GetLevel().ToString();
    }
}
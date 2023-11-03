using Godot;

public partial class PlayerGridMovementController : Node {

  [Signal] public delegate void OnFinishedMovingEventHandler();

  #region Properties set externally
  public TileMap ActiveTileMap { get; set; }
  public Character Character { get; set; }
  public StartMenu StartMenu { get; set; }
  #endregion

  public bool IsCharacterMoving { get; private set; } = false;
  public bool IsOverworldActive { get; set; } = false;

  private const int WALK_SPEED = 300;
  private const int RUN_SPEED = 600;

  private Vector2 _targetPosition;
  private int _moveSpeed = WALK_SPEED;
  private MoveDirection _lastDirection;
  private bool _isMovementDisabled = false;

  #region Lifecycle

  public override void _Process(double delta) {
    ReadInputMovement();
    MoveTowardsTargetPosition(delta);
  }

  public override void _UnhandledInput(InputEvent ev) {
    HandleOverworldActions(ev);
  }

  public void HandleClosingStartMenu() {
    StartMenu.HideControl();
    _isMovementDisabled = false;
  }

  private void HandleOverworldActions(InputEvent ev) {
    if (!IsOverworldActive) return;

    // Toggle Start Menu
    if (ev.IsActionPressed(Constants.InputActions.START)) {
        StartMenu.ShowControl();
      _isMovementDisabled = true;
    }
  }

  #endregion Lifecycle

  #region Public
  public void DisableMovement(bool isDisabled) {
    _isMovementDisabled = isDisabled;
  }
  #endregion

  private void ReadInputMovement() {
    if (_isMovementDisabled) return;

    if (Input.IsActionPressed(Constants.InputActions.UP)) {
      HandleMoveRequest(MoveDirection.Up);
    }
    if (Input.IsActionPressed(Constants.InputActions.DOWN)) {
      HandleMoveRequest(MoveDirection.Down);
    }
    if (Input.IsActionPressed(Constants.InputActions.LEFT)) {
      HandleMoveRequest(MoveDirection.Left);
    }
    if (Input.IsActionPressed(Constants.InputActions.RIGHT)) {
      HandleMoveRequest(MoveDirection.Right);
    }
    if (Input.IsActionPressed(Constants.InputActions.CANCEL)) {
      _moveSpeed = RUN_SPEED;
    } else {
      _moveSpeed = WALK_SPEED;
    }
  }

  private void MoveTowardsTargetPosition(double delta) {
    // Exit early if we aren't trying to reach target
    if (!IsCharacterMoving) return;

    // Exit if we reached destination
    if (_targetPosition == Character.GlobalPosition) {
      IsCharacterMoving = false;
      EmitSignal(SignalName.OnFinishedMoving);
      Character.AnimateIdle(_lastDirection);
      return;
    }

    Character.GlobalPosition = Character.GlobalPosition.MoveToward(
      _targetPosition, (float)(_moveSpeed * delta));
  }

  private void CalculateNextMove(MoveDirection direction) {
    Vector2 movement = GetMoveVector(direction);
    var globalCharEndPosition = Character.GlobalPosition + (movement * ActiveTileMap.Scale * ActiveTileMap.CellQuadrantSize);
    var endPositionlocalToGrid = ActiveTileMap.ToLocal(globalCharEndPosition);
    var gridPosition = ActiveTileMap.LocalToMap(endPositionlocalToGrid);

    var tileDataGround = ActiveTileMap.GetCellTileData(Constants.TileMapLayer.WALKABLE, gridPosition);
    if (tileDataGround == null) { // No ground tile exists, ignore more
      return;
    }
    if (tileDataGround.GetCustomData("is_wall").AsBool()) {
      return;
    }

    var tileDataWall = ActiveTileMap.GetCellTileData(Constants.TileMapLayer.WALLS, gridPosition);
    if (tileDataWall != null) { // we hit a wall
      return;
    }

    _targetPosition = globalCharEndPosition;
  }

  public void HandleMoveRequest(MoveDirection direction) {
    // ignore move commands until we are done moving
    if (IsCharacterMoving) return;

    CalculateNextMove(direction);
    IsCharacterMoving = true;
    Character.AnimateWalk(direction);
    _lastDirection = direction;
  }

  private Vector2 GetMoveVector(MoveDirection direction) {
    switch (direction) {
      case MoveDirection.Up:
        return Vector2.Up;
      case MoveDirection.Down:
        return Vector2.Down;
      case MoveDirection.Left:
        return Vector2.Left;
      case MoveDirection.Right:
        return Vector2.Right;
    };

    return Vector2.Zero;
  }


}
using Godot;

public partial class Character : Node2D {
  [Signal] public delegate void OnFinishedMovingEventHandler();

  [Export] private PackedScene PlayerStartMenu;

  public bool IsCharacterMoving { get; private set; }
  public bool IsOverworldActive { get; set; }

  private RayCast2D _raycast;
  private ShapeCast2D _shapeCast;
  private AnimatedSprite2D _animator;
  private Node _canvasLayer;
  private PlayerStartMenu _startMenu;

  private Vector2 _targetPosition;
  private MoveDirection _lastDirection;
  private int _moveSpeed = WALK_SPEED;
  private bool _isMovementDisabled = true;
  private bool _checkForItem = false;

  private const int WALK_SPEED = 300;
  private const int RUN_SPEED = 600;

  #region Lifecycle
  public override void _Ready() {
    _animator = this.GetNodeOrDefault<AnimatedSprite2D>("%Animator");
    _shapeCast = this.GetNodeOrDefault<ShapeCast2D>("%ShapeCast2D");
    _canvasLayer = this.GetNodeOrDefault("%CanvasLayer");

    Scale = Constants.PIXEL_SCALE;
  }

  public override void _Process(double delta) {
    ReadInput();
    MoveTowardsTargetPosition(delta);
  }

  public override void _UnhandledInput(InputEvent ev) {
    if (ev.IsActionPressed(Constants.InputActions.START)) {
      ToggleStartMenu();
    }
  }

  public override void _PhysicsProcess(double delta) {
    if (!_checkForItem) return;
    // this is a space check so there should only be 1 thing to collide with at a time
    // var thing = _shapeCast.GetCollider(0) as TileMapItem;
    // thing?.Activate();

    _checkForItem = false;
  }

  public void HandleClosingStartMenu() {
    // StartMenu.HideControl();
    _isMovementDisabled = false;
  }

  #endregion Lifecycle

  #region Public Methods
  public void DisableMovement(bool isDisabled) {
    _isMovementDisabled = isDisabled;
  }

  public Vector2I GetGlobalPlayerGridLocation() {
    var tileMap = Autoload.MapController.ActiveTileMap;
    var localMapPosition = tileMap.ToLocal(GlobalPosition);
    return tileMap.LocalToMap(localMapPosition);
  }

  public void HandleMoveRequest(MoveDirection direction) {
    // ignore move commands until we are done moving
    if (IsCharacterMoving) return;

    CalculateNextMove(direction);
    IsCharacterMoving = true;
    AnimateWalk(direction);
    _lastDirection = direction;
  }
  #endregion Public Methods

  #region Private Methods
  private void ToggleStartMenu() {
    if (_startMenu == null) {
      DisableMovement(true);
      _startMenu = PlayerStartMenu.Instantiate<PlayerStartMenu>();
      _canvasLayer.AddChild(_startMenu);
      _startMenu.TreeExited += () => DisableMovement(false);
      return;
    }

    _startMenu.Visible = !_startMenu.Visible;
    DisableMovement(_startMenu.Visible);
    if (_startMenu.Visible) {
      _startMenu.SetFocusOnFirstItem();
    }
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
  private void CalculateNextMove(MoveDirection direction) {
    Vector2 movement = GetMoveVector(direction);
    var tileMap = Autoload.MapController.ActiveTileMap;
    var globalCharEndPosition = GlobalPosition + (movement * tileMap.Scale * tileMap.CellQuadrantSize);
    var endPositionlocalToGrid = tileMap.ToLocal(globalCharEndPosition);
    var endGridPosition = tileMap.LocalToMap(endPositionlocalToGrid);

    var targetPosTileData = tileMap.GetCellTileData(Constants.TileMapLayer.WALKABLE, endGridPosition);
    if (targetPosTileData == null) { // No ground tile exists, ignore more
      return;
    }
    if (targetPosTileData.GetCustomData(Constants.TileMapProperties.IS_WALL).AsBool()) {
      return;
    }
    if (targetPosTileData.GetCustomData(Constants.TileMapProperties.IS_OCCUPIED).AsBool()) {
      return;
    }
    if (targetPosTileData.GetCustomData(Constants.TileMapProperties.IS_WATER).AsBool()) {
      // eventually handle ability to swim with HM
      return;
    }

    var tileDataWall = tileMap.GetCellTileData(Constants.TileMapLayer.WALLS, endGridPosition);
    if (tileDataWall != null) { // we hit a wall
      return;
    }

    _targetPosition = globalCharEndPosition;
  }

  private void ReadInput() {
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
    if (Input.IsActionJustPressed(Constants.InputActions.ACCEPT)) {
      _checkForItem = true;
    }
  }

  private void MoveTowardsTargetPosition(double delta) {
    // Exit early if we aren't trying to reach target
    if (!IsCharacterMoving) return;

    // Exit if we reached destination
    if (_targetPosition == GlobalPosition) {
      var tileMap = Autoload.MapController.ActiveTileMap;
      IsCharacterMoving = false;
      EmitSignal(SignalName.OnFinishedMoving);
      AnimateIdle(_lastDirection);
      _shapeCast.GlobalPosition = GlobalPosition + (GetMoveVector(_lastDirection) * tileMap.Scale * tileMap.CellQuadrantSize);
      return;
    }

    GlobalPosition = GlobalPosition.MoveToward(
      _targetPosition, (float)(_moveSpeed * delta));
  }

  #endregion Private Methods

  public void AnimateWalk(MoveDirection direction) {
    switch (direction) {
      case MoveDirection.Up:
        _animator.Animation = "walk_up";
        _animator.Play();
        return;

      case MoveDirection.Down:
        _animator.Animation = "walk_down";
        _animator.Play();
        return;

      case MoveDirection.Left:
        _animator.Animation = "walk_left";
        _animator.Play();
        return;

      case MoveDirection.Right:
        _animator.Animation = "walk_right";
        _animator.Play();
        return;
    }
  }

  public void AnimateIdle(MoveDirection direction) {
    switch (direction) {
      case MoveDirection.Up:
        _animator.Animation = "idle_up";
        _animator.Play();
        return;

      case MoveDirection.Down:
        _animator.Animation = "idle_down";
        _animator.Play();
        return;

      case MoveDirection.Left:
        _animator.Animation = "idle_left";
        _animator.Play();
        return;

      case MoveDirection.Right:
        _animator.Animation = "idle_right";
        _animator.Play();
        return;
    }
  }
}

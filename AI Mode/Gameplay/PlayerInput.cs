using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private enum MoveType
    {
        rot0,
        rot90,
        rot180,
        rot270
    }

    public bool ActiveMouse { get; set; } = true;
    public bool ActiveKeyboard { get; set; } = true;

    [SerializeField] private Field field;
    [SerializeField] private CameraMovements fieldCamera;
    [SerializeField] private BGAnimationAIMode bgAnimationPlayer;
    [SerializeField] private BGAnimationAIMode bgAnimationAgent;
    [SerializeField] private FieldAnimationAIMode fieldAnimation;

    private MoveType moveType = MoveType.rot0;

    private Vector3 moveW = Vector3.forward;
    private Vector3 moveS = Vector3.back;
    private Vector3 moveA = Vector3.left;
    private Vector3 moveD = Vector3.right;

    private Vector3 rotateW = new Vector3(90.0f, 0.0f, 0.0f);
    private Vector3 rotateS = new Vector3(-90.0f, 0.0f, 0.0f);
    private Vector3 rotateA = new Vector3(0.0f, 90.0f, 0.0f);
    private Vector3 rotateD = new Vector3(0.0f, -90.0f, 0.0f);
    private Vector3 rotateQ = new Vector3(0.0f, 0.0f, 90.0f);
    private Vector3 rotateE = new Vector3(0.0f, 0.0f, -90.0f);

    private void Start()
    {
        bgAnimationPlayer.WallAnimationHighlight(FieldBGAnimation.Wall.maxZ);
        bgAnimationAgent.WallAnimationHighlight(FieldBGAnimation.Wall.maxZ);
    }

    private void Update()
    {
        if (ActiveMouse) MouseInput();
        if (ActiveKeyboard) KeyboardInput();
    }

    private void MouseInput()
    {
        if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.defaultZoom])) fieldCamera.ClearRadius();
        else
        {
            float mouseWheel = Input.GetAxis("Mouse ScrollWheel") * 15;
            if (mouseWheel != 0) fieldCamera.AddRadius(-mouseWheel);
        }

        float xMove = Input.GetAxis("Mouse X") * DataManager.CameraSpeed / 2;
        float yMove = Input.GetAxis("Mouse Y") * DataManager.CameraSpeed / 2;

        bool mouseRightButtonDown = Input.GetKey(DataManager.Controls[DataControls.ControlsTypes.moveCamera]);
        if (!mouseRightButtonDown) fieldCamera.ClearOffsets();

        if (xMove != 0 || yMove != 0)
        {
            if (!mouseRightButtonDown)
            {
                if (DataManager.InvertedCameraX) xMove = -xMove;
                if (DataManager.InvertedCameraY) yMove = -yMove;

                fieldCamera.AddRotation(xMove, yMove);

                if (fieldCamera.Yaw >= -45.0f && fieldCamera.Yaw < 45.0f)
                {
                    if (moveType != MoveType.rot0)
                    {
                        moveType = MoveType.rot0;

                        moveW = Vector3.forward;
                        moveS = Vector3.back;
                        moveA = Vector3.left;
                        moveD = Vector3.right;

                        rotateW = new Vector3(90.0f, 0.0f, 0.0f);
                        rotateS = new Vector3(-90.0f, 0.0f, 0.0f);
                        rotateA = new Vector3(0.0f, 90.0f, 0.0f);
                        rotateD = new Vector3(0.0f, -90.0f, 0.0f);
                        rotateQ = new Vector3(0.0f, 0.0f, 90.0f);
                        rotateE = new Vector3(0.0f, 0.0f, -90.0f);

                        bgAnimationPlayer.WallAnimationHighlight(FieldBGAnimation.Wall.maxZ);
                        bgAnimationAgent.WallAnimationHighlight(FieldBGAnimation.Wall.maxZ);
                        fieldAnimation.ClearAllCuts(false);
                    }
                }
                else if (fieldCamera.Yaw >= 45.0f && fieldCamera.Yaw < 135.0f)
                {
                    if (moveType != MoveType.rot90)
                    {
                        moveType = MoveType.rot90;

                        moveW = Vector3.left;
                        moveS = Vector3.right;
                        moveA = Vector3.back;
                        moveD = Vector3.forward;

                        rotateW = new Vector3(0.0f, 0.0f, 90.0f);
                        rotateS = new Vector3(0.0f, 0.0f, -90.0f);
                        rotateA = new Vector3(0.0f, 90.0f, 0.0f);
                        rotateD = new Vector3(0.0f, -90.0f, 0.0f);
                        rotateQ = new Vector3(-90.0f, 0.0f, 0.0f);
                        rotateE = new Vector3(90.0f, 0.0f, 0.0f);

                        bgAnimationPlayer.WallAnimationHighlight(FieldBGAnimation.Wall.minX);
                        bgAnimationAgent.WallAnimationHighlight(FieldBGAnimation.Wall.minX);
                        fieldAnimation.ClearAllCuts(false);
                    }
                }
                else if (fieldCamera.Yaw >= 135.0f && fieldCamera.Yaw < 225.0f)
                {
                    if (moveType != MoveType.rot180)
                    {
                        moveType = MoveType.rot180;

                        moveW = Vector3.back;
                        moveS = Vector3.forward;
                        moveA = Vector3.right;
                        moveD = Vector3.left;

                        rotateW = new Vector3(-90.0f, 0.0f, 0.0f);
                        rotateS = new Vector3(90.0f, 0.0f, 0.0f);
                        rotateA = new Vector3(0.0f, 90.0f, 0.0f);
                        rotateD = new Vector3(0.0f, -90.0f, 0.0f);
                        rotateQ = new Vector3(0.0f, 0.0f, -90.0f);
                        rotateE = new Vector3(0.0f, 0.0f, 90.0f);

                        bgAnimationPlayer.WallAnimationHighlight(FieldBGAnimation.Wall.minZ);
                        bgAnimationAgent.WallAnimationHighlight(FieldBGAnimation.Wall.minZ);
                        fieldAnimation.ClearAllCuts(false);
                    }
                }
                else
                {
                    if (moveType != MoveType.rot270)
                    {
                        moveType = MoveType.rot270;

                        moveW = Vector3.right;
                        moveS = Vector3.left;
                        moveA = Vector3.forward;
                        moveD = Vector3.back;

                        rotateW = new Vector3(0.0f, 0.0f, -90.0f);
                        rotateS = new Vector3(0.0f, 0.0f, 90.0f);
                        rotateA = new Vector3(0.0f, 90.0f, 0.0f);
                        rotateD = new Vector3(0.0f, -90.0f, 0.0f);
                        rotateQ = new Vector3(90.0f, 0.0f, 0.0f);
                        rotateE = new Vector3(-90.0f, 0.0f, 0.0f);

                        bgAnimationPlayer.WallAnimationHighlight(FieldBGAnimation.Wall.maxX);
                        bgAnimationAgent.WallAnimationHighlight(FieldBGAnimation.Wall.maxX);
                        fieldAnimation.ClearAllCuts(false);
                    }
                }
            }
            else
            {
                fieldCamera.AddOffset(xMove, yMove);
            }
        }
    }

    private void KeyboardInput()
    {
        if (Input.GetKey(DataManager.Controls[DataControls.ControlsTypes.rotatePieceMode]))
        {
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveForward])) field.RotatePiece(rotateW);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveBackward])) field.RotatePiece(rotateS);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveLeft])) field.RotatePiece(rotateA);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveRight])) field.RotatePiece(rotateD);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.rotatePieceCounterclockwise])) field.RotatePiece(rotateQ);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.rotatePieceClockwise])) field.RotatePiece(rotateE);
        }
        else
        {
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveForward])) field.MovePieceSide(moveW);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveBackward])) field.MovePieceSide(moveS);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveLeft])) field.MovePieceSide(moveA);
            if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.moveRight])) field.MovePieceSide(moveD);
        }

        if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.cutMore]))
        {
            if (moveType == MoveType.rot0) fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.zMin, 1.0f);
            else if (moveType == MoveType.rot90) fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.xMax, -1.0f);
            else if (moveType == MoveType.rot180) fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.zMax, -1.0f);
            else fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.xMin, 1.0f);
        }

        if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.cutLess]))
        {
            if (moveType == MoveType.rot0) fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.zMin, -1.0f);
            else if (moveType == MoveType.rot90) fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.xMax, 1.0f);
            else if (moveType == MoveType.rot180) fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.zMax, 1.0f);
            else fieldAnimation.AddCut(FieldAnimationAIMode.Cuts.xMin, -1.0f);
        }

        if (Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.cutDefault])) fieldAnimation.ClearAllCuts();
    }
}

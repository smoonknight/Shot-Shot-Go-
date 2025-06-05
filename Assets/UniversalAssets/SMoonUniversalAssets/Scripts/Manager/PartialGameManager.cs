using UnityEngine;

public partial class GameManager : SingletonWithDontDestroyOnLoad<GameManager>
{
    public GameInfoScriptableObject gameInfo;

    public string GetPlayerName() => "Hisa";
    public string GameVersion => gameInfo.gameVersion;

    public void InitAllAction()
    {

    }

    public void SetCursor(bool isShow)
    {
        Cursor.visible = isShow;
        Cursor.lockState = isShow ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

public struct PlayerStatus
{

}
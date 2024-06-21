

using Nakama;
using Nakama.TinyJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class OpCodes
{
    public const long VelocityAndPosition = 1;
    public const long Input = 2;
    public const long Died = 3;
    public const long Respawned = 4;
    public const long NewRound = 5;
    public const long DiceIndex = 6;

}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public NakamaConnection NakamaConnection;
    private IDictionary<string, string> players;
    private IUserPresence localUser;
    private IMatch currentMatch;
    public Sprite[] diceSprites = new Sprite[6];
    public Image Face;
    public bool myturn = false;
    [SerializeField] GameObject dice;
    [SerializeField] GameObject home, game;
    [SerializeField] Animator _dice;
    [SerializeField] UISpriteAnimation uISpriteAnimation;
    private void Awake()
    {
        Instance = this;
    }
    private async void Start()
    {
        // Create an empty dictionary to hold references to the currently connected players.
        players = new Dictionary<string, string>();
        await NakamaConnection.Connect();
        var mainThread = UnityMainThreadDispatcher.Instance();
        NakamaConnection.Socket.ReceivedMatchmakerMatched += m => mainThread.Enqueue(() => OnReceivedMatchmakerMatched(m));
        NakamaConnection.Socket.ReceivedMatchPresence += m => mainThread.Enqueue(() => OnReceivedMatchPresence(m));
        NakamaConnection.Socket.ReceivedMatchState += m => mainThread.Enqueue(async () => await OnReceivedMatchState(m));

    }
    private async void OnReceivedMatchmakerMatched(IMatchmakerMatched matched)
    {
        home.SetActive(false);
        game.SetActive(true);
        // Cache a reference to the local user.
        localUser = matched.Self.Presence;
        Debug.Log(localUser);
        // Join the match.
        var match = await NakamaConnection.Socket.JoinMatchAsync(matched);
        foreach (var user in match.Presences)
        {
            Player(match.Id, user);
        }

        // Cache a reference to the current match.
        currentMatch = match;
    }
    private void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
    {

        // For each new user that joins, spawn a player for them.
        foreach (var user in matchPresenceEvent.Joins)
        {
            Player(matchPresenceEvent.MatchId, user);
        }
    }
    private async Task OnReceivedMatchState(IMatchState matchState)
    {

        // Get the local user's session ID.
        var userSessionId = matchState.UserPresence.SessionId;

        // If the matchState object has any state length, decode it as a Dictionary.
        var state = matchState.State.Length > 0 ? System.Text.Encoding.UTF8.GetString(matchState.State).FromJson<Dictionary<string, string>>() : null;
        Debug.Log(state);
        // Decide what to do based on the Operation Code as defined in OpCodes.
        switch (matchState.OpCode)
        {
            case
                OpCodes.DiceIndex:
                string data = state["DiceIndex"];
                DiceRollRpc(data);
                break;
            default:
                break;
        }
    }
    public void callIt(string diceIndex)
    {
        Debug.Log(diceIndex)
;
    }
    private void Player(string matchId, IUserPresence user, int spawnIndex = -1)
    {

        if (players.ContainsKey(user.SessionId))
        {
            return;
        }


        var isLocal = user.SessionId == localUser.SessionId;
        players.Add(user.SessionId, null);
        if (!isLocal)
        {
            Debug.Log(
                "NOT LOCAL PLAYER");
        }
        if (isLocal)
        {
            Debug.Log("LOCAL Player");
        }


    }
    public void SendMatchState(long opCode, string state)
    {
        NakamaConnection.Socket.SendMatchStateAsync(currentMatch.Id, opCode, state);
    }
    public async void FindMatch()
    {


        PlayerPrefs.SetString("Name", "NAME" + UnityEngine.Random.Range(10, 20));
        await NakamaConnection.FindMatch();
    }
    public static string DiceIndex(int spawnIndex)
    {
        var values = new Dictionary<string, string>
        {
            { "DiceIndex", spawnIndex.ToString() },
        };

        return values.ToJson();
    }

    public void DiceRoll()
    {
        uISpriteAnimation.Func_PlayUIAnim();
        Invoke(nameof(DelayDice), .7f);

    }
    public void DiceRollRpc(string index)
    {
        uISpriteAnimation.Func_PlayUIAnim();
        _ = StartCoroutine(DelayRpcCall(index));

    }

    void DelayDiceRpcCall(string index)
    {
        int value = int.Parse(index);
        Face.sprite = diceSprites[value - 1];
    }
    void DelayDice()
    {
        int value = UnityEngine.Random.Range(0, 6);
        Face.sprite = diceSprites[value];
        Debug.Log(value + 1);
        SendMatchState(OpCodes.DiceIndex, DiceIndex(value + 1));
    }
    IEnumerator DelayRpcCall(string index)
    {
        yield return new WaitForSeconds(.7f);
        DelayDiceRpcCall(index);
    }
}

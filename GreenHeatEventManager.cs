using UnityEngine;
using WebSocketSharp;
public class GreenHeatEventManager : MonoBehaviour
{
    public delegate void GreenHeatClick(GreenHeatMessage message);
    public static event GreenHeatClick OnGreenHeatClick;
    public delegate void GreenHeatRelease(GreenHeatMessage message);
    public static event GreenHeatRelease OnGreenHeatRelease;
    public delegate void GreenHeatHover(GreenHeatMessage message);
    public static event GreenHeatHover OnGreenHeatHover;
    public delegate void GreenHeatDrag(GreenHeatMessage message);
    public static event GreenHeatDrag OnGreenHeatDrag;

    private bool isConnecting = false;
    private WebSocket ws;

    private string url = "wss://heat.prod.kr/ratcousin";
    
    private static GreenHeatEventManager _instance;

    public static GreenHeatEventManager Instance { get { return _instance; } }


    private void Awake() //making it persist across scenes
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        } else {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        Connect();
    }

    private void Connect()
    {
        if (isConnecting) return;
        isConnecting = true;
        ws = new WebSocket(url);
        ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Connection open!");
            isConnecting = false;
        };

        ws.OnError += (sender, e) =>
        {
            Debug.Log("Error! " + e.Exception);

            isConnecting = false;
        };
        
        ws.OnMessage += (sender, e) =>
        {
            var message = GreenHeatMessage.CreateFromJson(e.Data);
            switch (message.type)
            {
                case "click":
                    OnGreenHeatClick?.Invoke(message);
                    break;
                case "release":
                    OnGreenHeatRelease?.Invoke(message);
                    break;
                case "hover":
                    OnGreenHeatHover?.Invoke(message);
                    break;
                case "drag":
                    OnGreenHeatDrag?.Invoke(message);
                    break;
            }

        };
        ws.Connect();

    }

    private void Update()
    {
        if (ws?.ReadyState != WebSocketState.Open && ws?.ReadyState != WebSocketState.Connecting) //in update loop because otherwise it doesn't reconnect if it disconnects when tabbed out
        {
            Debug.Log("Connection closed! Attempting reconnection.");
            isConnecting = false;
            Connect();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quit, disconnecting from greenheat.");
        ws.Close();
    }
}

[System.Serializable]
public class GreenHeatMessage
{
    public string id;
    public float x;
    public float y;
    public string type; //can be "click", "release", "drag", or "hover"
    public string button; //can be "left", "right", "middle"
    public bool shift;
    public bool ctrl;
    public bool alt;
    public long time; // timestamp in ms
    public long latency; //latency between viewer and streamer
    
    public static GreenHeatMessage CreateFromJson(string jsonString)
    {
        return JsonUtility.FromJson<GreenHeatMessage>(jsonString);
    }
}
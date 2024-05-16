using System.Collections.Generic;
using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class mqttReceiver : M2MqttUnityClient
{
    [Header("MQTT topics")]
    [Tooltip("Set the topic to subscribe. !!!ATTENTION!!! multi-level wildcard # subscribes to all topics")]
    public string topicVelocity = "velocidad"; // Tópico para la velocidad
    public string topicSteering = "pendiente"; // Tópico para el giro

    public delegate void OnMessageArrivedDelegate(string newMsg);

    // Eventos que utilizan el delegado definido anteriormente.
    public event OnMessageArrivedDelegate OnVelocityMessageArrived;
    public event OnMessageArrivedDelegate OnSteeringMessageArrived;
    public event OnMessageArrivedDelegate OnMessageArrived;
    

    [Tooltip("Set the topic to publish (optional)")]
    public string topicPublish = ""; // topic to publish
    public string messagePublish = ""; // message to publish

    [Tooltip("Set this to true to perform a testing cycle automatically on startup")]
    public bool autoTest = false;

    //using C# Property GET/SET and event listener to reduce Update overhead in the controlled objects
    private string m_msg;
    public string msg
    {
        get { return m_msg; }
        set
        {
            if (m_msg == value) return;
            m_msg = value;
            OnMessageArrived?.Invoke(m_msg);
        }
    }
    

    //using C# Property GET/SET and event listener to expose the connection status
    private bool m_isConnected;
    public bool isConnected
    {
        get { return m_isConnected; }
        set
        {
            if (m_isConnected == value) return;
            m_isConnected = value;
            OnConnectionSucceeded?.Invoke(isConnected);
        }
    }

    public delegate void OnConnectionSucceededDelegate(bool isConnected);
    public event OnConnectionSucceededDelegate OnConnectionSucceeded;

    // a list to store the messages
    private List<string> eventMessages = new List<string>();
/////////////////PUBLICAR INCLINACIÓN
public void PublishStart(string topic, string message)
{
    if (client != null && client.IsConnected)
    {
        client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        Debug.Log("Published to topic " + topic + ": " + message);
    }
    else
    {
        Debug.LogWarning("MQTT client not connected. Cannot publish message.");
    }
}
//PUBLICAR GIROAPP
public void PublishCompassHeading(string topic, int compassHeading)
{
    if (client != null && client.IsConnected)
    {
        string message = compassHeading.ToString();
        client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        Debug.Log("Published to topic " + topic + ": " + message);
    }
    else
    {
        Debug.LogWarning("MQTT client not connected. Cannot publish message.");
    }
}
//
//
    public void Publish()
    {
        client.Publish(topicPublish, System.Text.Encoding.UTF8.GetBytes(messagePublish), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        Debug.Log("Test message published");
    }

    public void SetEncrypted(bool isEncrypted)
    {
        this.isEncrypted = isEncrypted;
    }

    protected override void OnConnecting()
    {
        base.OnConnecting();
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        isConnected = true;
        if (autoTest)
        {
            Publish();
        }
    }

    protected override void OnConnectionFailed(string errorMessage)
    {
        Debug.Log("CONNECTION FAILED! " + errorMessage);
    }

    protected override void OnDisconnected()
    {
        Debug.Log("Disconnected.");
        isConnected = false;
    }

    protected override void OnConnectionLost()
    {
        Debug.Log("CONNECTION LOST!");
    }

    protected override void SubscribeTopics()
    {
        client.Subscribe(new string[] { topicVelocity, topicSteering }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
    }

    protected override void UnsubscribeTopics()
    {
        client.Unsubscribe(new string[] { topicVelocity, topicSteering });
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = System.Text.Encoding.UTF8.GetString(message);
        Debug.Log("Received: " + msg);
        Debug.Log("from topic: " + topic);
        if (topic == topicVelocity)
        {
            OnVelocityMessageArrived?.Invoke(msg);
        }
        else if (topic == topicSteering)
        {
            OnSteeringMessageArrived?.Invoke(msg);
        }
    }

    private void StoreMessage(string eventMsg)
    {
        if (eventMessages.Count > 50)
        {
            eventMessages.Clear();
        }
        eventMessages.Add(eventMsg);
    }

    protected override void Update()
    {
        base.Update(); // call ProcessMqttEvents()
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnValidate()
    {
        if (autoTest)
        {
            autoConnect = true;
        }
    }
}
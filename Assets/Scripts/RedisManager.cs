using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using StackExchange.Redis;

public class RedisChannels
{
    public static string robot_qd = "encoder_speeds";
    public static string robot_q = "encoder_positions";
    public static string robot_q_non_rt = "__encoder_positions";
    public static string stroke_speed = "caresse";
    public static string stimulus_done = "stimulus_done";
    public static string user_gave_input = "user_gave_input";
    public static string current_trial = "current_trial";
    public static string sleeping = "sleeping";
    public static string input_curr_selected = "input_curr_selected";
    public static string pleasantness = "pleasantness";
    public static string intensity = "intensity";
}

public class RedisManager : MonoBehaviour
{
    public string redisConnectionString = "localhost:6379";
    public Dictionary<string, string> Channels = new(){
        {"robot_qd", "encoder_speeds"},
        {"robot_q", "encoder_positions"},
        {"robot_q_non_rt", "__encoder_positions"},
        {"stroke_speed", "caresse"},
        {"stimulus_done", "stimulus_done"},
        {"user_gave_input", "user_gave_input"},
        {"current_trial", "current_trial"},
        {"sleeping", "sleeping"},
    };

    private ConnectionMultiplexer connection;
    public bool connected;

    void TryRedisConnect()
    {
        try
        {
            connection = ConnectionMultiplexer.Connect(redisConnectionString);
            connected = true;
        }
        catch (RedisConnectionException)
        {
            connected = false;
        }
    }

    public void Publish(string channel, string message)
    {
        if (connected)
        {
            var pubsub = connection.GetSubscriber();
            RedisChannel c = new(channel, RedisChannel.PatternMode.Literal);

            pubsub.Publish(c, message, CommandFlags.FireAndForget);
        }
    }

    public void Set(string key, string value)
    {
        if (connected)
        {
            IDatabase db = connection.GetDatabase();
            db.StringSet(key, value);
        }
    }

    public string Get(string key)
    {
        if (connected)
        {
            IDatabase db = connection.GetDatabase();
            return db.StringGet(key).ToString();
        }

        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        TryRedisConnect();

        if (connected)
        {
            RedisChannel channel = new("test", RedisChannel.PatternMode.Literal);
            var pubsub = connection.GetSubscriber();
            pubsub.Publish(channel, "This is a test message!!", CommandFlags.FireAndForget);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
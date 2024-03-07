using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using StackExchange.Redis;

public class RedisManager : MonoBehaviour
{
    public string redisConnectionString = "localhost:6379";
    private ConnectionMultiplexer connection;
    private bool connected;
   
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
        if (!connected)
        {
            TryRedisConnect();
            return;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class City
{
    [SerializeField] private string name;
    [SerializeField] private string id;
    [SerializeField] private Sprite logoSprite;

    public string Name { get => name; set => name = value; }
    public string ID { get => id; set => id = value; }
    public Sprite LogoSprite { get => logoSprite; set => logoSprite = value; }

    public City(string name, string id, Sprite logoSprite = null)
    {
        this.name = name;
        this.id = id;
        this.logoSprite = logoSprite;
    }
}

[Serializable]
public class SocialMediaMetrics
{
    [SerializeField] private int instagramFollowers;
    [SerializeField] private int tikTokFollowers;
    [SerializeField] private int tikTokLikes;
    [SerializeField] private City associatedCity;
    [SerializeField] private DateTime timestamp;

    public int InstagramFollowers { get => instagramFollowers; set => instagramFollowers = value; }
    public int TikTokFollowers { get => tikTokFollowers; set => tikTokFollowers = value; }
    public int TikTokLikes { get => tikTokLikes; set => tikTokLikes = value; }
    public City AssociatedCity { get => associatedCity; set => associatedCity = value; }
    public DateTime Timestamp { get => timestamp; set => timestamp = value; }

    public SocialMediaMetrics(int instagramFollowers, int tikTokFollowers, int tikTokLikes, City associatedCity, DateTime timestamp)
    {
        this.instagramFollowers = instagramFollowers;
        this.tikTokFollowers = tikTokFollowers;
        this.tikTokLikes = tikTokLikes;
        this.associatedCity = associatedCity;
        this.timestamp = timestamp;
    }

    public void UpdateFromRawData(Dictionary<string, string> rawData)
    {
        if (rawData.TryGetValue("instagram_followers", out string igFollowers))
        {
            int.TryParse(igFollowers, out instagramFollowers);
        }
        
        if (rawData.TryGetValue("tiktok_followers", out string ttFollowers))
        {
            int.TryParse(ttFollowers, out tikTokFollowers);
        }
        
        if (rawData.TryGetValue("tiktok_likes", out string ttLikes))
        {
            int.TryParse(ttLikes, out tikTokLikes);
        }
        
        if (rawData.TryGetValue("timestamp", out string timestampStr))
        {
            DateTime.TryParse(timestampStr, out timestamp);
        }
    }
}

[Serializable]
public class EventMetrics
{
    [SerializeField] private int ticketsSold;
    [SerializeField] private float averageAttendance;
    [SerializeField] private int numberOfEvents;
    [SerializeField] private City associatedCity;
    [SerializeField] private DateTime timestamp;

    public int TicketsSold { get => ticketsSold; set => ticketsSold = value; }
    public float AverageAttendance { get => averageAttendance; set => averageAttendance = value; }
    public int NumberOfEvents { get => numberOfEvents; set => numberOfEvents = value; }
    public City AssociatedCity { get => associatedCity; set => associatedCity = value; }
    public DateTime Timestamp { get => timestamp; set => timestamp = value; }

    public EventMetrics(int ticketsSold, float averageAttendance, int numberOfEvents, City associatedCity, DateTime timestamp)
    {
        this.ticketsSold = ticketsSold;
        this.averageAttendance = averageAttendance;
        this.numberOfEvents = numberOfEvents;
        this.associatedCity = associatedCity;
        this.timestamp = timestamp;
    }

    public void UpdateFromRawData(Dictionary<string, string> rawData)
    {
        if (rawData.TryGetValue("tickets_sold", out string tickets))
        {
            int.TryParse(tickets, out ticketsSold);
        }
        
        if (rawData.TryGetValue("average_attendance", out string attendance))
        {
            float.TryParse(attendance, out averageAttendance);
        }
        
        if (rawData.TryGetValue("number_of_events", out string events))
        {
            int.TryParse(events, out numberOfEvents);
        }
        
        if (rawData.TryGetValue("timestamp", out string timestampStr))
        {
            DateTime.TryParse(timestampStr, out timestamp);
        }
    }
}

public class DataModelClasses : MonoBehaviour
{
    [SerializeField] private List<City> cities = new List<City>();
    [SerializeField] private List<SocialMediaMetrics> socialMediaMetrics = new List<SocialMediaMetrics>();
    [SerializeField] private List<EventMetrics> eventMetrics = new List<EventMetrics>();

    public List<City> Cities => cities;
    public List<SocialMediaMetrics> SocialMediaMetrics => socialMediaMetrics;
    public List<EventMetrics> EventMetrics => eventMetrics;

    // Retrieve a city by ID
    public City GetCityById(string id)
    {
        return cities.Find(c => c.ID == id);
    }

    // Add data methods
    public void AddCity(City city)
    {
        cities.Add(city);
    }

    public void AddSocialMediaMetrics(SocialMediaMetrics metrics)
    {
        socialMediaMetrics.Add(metrics);
    }

    public void AddEventMetrics(EventMetrics metrics)
    {
        eventMetrics.Add(metrics);
    }

    // Get the latest metrics for a specific city
    public SocialMediaMetrics GetLatestSocialMediaMetrics(string cityId)
    {
        return socialMediaMetrics
            .FindAll(m => m.AssociatedCity.ID == cityId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();
    }

    public EventMetrics GetLatestEventMetrics(string cityId)
    {
        return eventMetrics
            .FindAll(m => m.AssociatedCity.ID == cityId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();
    }
}

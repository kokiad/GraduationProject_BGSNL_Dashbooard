using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private DataModelClasses dataModel;
    [SerializeField] private GoogleSheetsService sheetsService;
    
    [Header("BGSNL Social Media Metrics")]
    [SerializeField] private TextMeshProUGUI instagramFollowersText;
    [SerializeField] private TextMeshProUGUI tiktokFollowersText;
    [SerializeField] private TextMeshProUGUI tiktokLikesText;
    
    [Header("BGSNL Event Metrics")]
    [SerializeField] private TextMeshProUGUI ticketsSoldText;
    [SerializeField] private TextMeshProUGUI averageAttendanceText;
    [SerializeField] private TextMeshProUGUI numberOfEventsText;
    
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI lastUpdateText;
    
    private bool hasUpdatedUI = false;
    private bool isInitialized = false;
    
    private void Awake()
    {
        Debug.Log("[UIManager] Initializing...");
        
        // Ensure we only have one UIManager
        UIManager[] managers = FindObjectsOfType<UIManager>();
        if (managers.Length > 1)
        {
            Debug.LogWarning("[UIManager] Multiple UIManager instances detected! Destroying this one.");
            Destroy(gameObject);
            return;
        }
        
        // Find references if not set in inspector
        if (dataModel == null)
        {
            dataModel = FindObjectOfType<DataModelClasses>();
            if (dataModel == null)
            {
                Debug.LogError("[UIManager] Could not find DataModelClasses!");
            }
            else
            {
                Debug.Log("[UIManager] Found DataModelClasses reference.");
            }
        }
        
        if (sheetsService == null)
        {
            sheetsService = FindObjectOfType<GoogleSheetsService>();
            if (sheetsService == null)
            {
                Debug.LogError("[UIManager] Could not find GoogleSheetsService!");
            }
            else
            {
                Debug.Log("[UIManager] Found GoogleSheetsService reference.");
            }
        }
        
        isInitialized = true;
        Debug.Log("[UIManager] Initialization complete.");
    }
    
    private void Start()
    {
        if (!isInitialized) return;
        
        Debug.Log("[UIManager] Start method called. Setting up update sequence...");
        
        // Wait longer for the data to fully load
        StartCoroutine(WaitAndUpdateDashboard());
    }
    
    private IEnumerator WaitAndUpdateDashboard()
    {
        Debug.Log("[UIManager] Waiting for data to load...");
        
        // Wait for data loading to complete
        yield return new WaitForSeconds(1.5f);
        
        // Update the dashboard
        Debug.Log("[UIManager] Wait complete, updating dashboard...");
        UpdateBGSNLDashboard();
    }
    
    /// <summary>
    /// Updates the BGSNL dashboard UI with the latest metrics
    /// </summary>
    public void UpdateBGSNLDashboard()
    {
        if (!isInitialized)
        {
            Debug.LogError("[UIManager] Cannot update dashboard - not properly initialized!");
            return;
        }
        
        if (hasUpdatedUI)
        {
            Debug.Log("[UIManager] Dashboard already updated once, skipping to avoid overrides.");
            return;
        }
        
        if (dataModel == null)
        {
            Debug.LogError("[UIManager] Cannot update dashboard - DataModel is null!");
            return;
        }
        
        Debug.Log("[UIManager] Beginning dashboard update...");
        
        // Check if we have any cities in the data model
        if (dataModel.Cities.Count == 0)
        {
            Debug.LogWarning("[UIManager] No cities found in DataModelClasses");
            return;
        }
        
        // Use the first city in the list (assuming this is your BGSNL city)
        City bgsnlCity = dataModel.Cities[0];
        string cityId = bgsnlCity.ID;
        
        Debug.Log($"[UIManager] Looking for metrics for city: {bgsnlCity.Name} (ID: {cityId})");
        
        // Get metrics for BGSNL
        SocialMediaMetrics socialMetrics = dataModel.GetLatestSocialMediaMetrics(cityId);
        EventMetrics eventMetrics = dataModel.GetLatestEventMetrics(cityId);
        
        // Update social media metrics
        if (socialMetrics != null)
        {
            Debug.Log($"[UIManager] Found social media metrics: Instagram={socialMetrics.InstagramFollowers}, TikTok={socialMetrics.TikTokFollowers}, Likes={socialMetrics.TikTokLikes}");
            
            if (instagramFollowersText != null)
            {
                instagramFollowersText.text = socialMetrics.InstagramFollowers.ToString("N0");
                Debug.Log($"[UIManager] Set Instagram followers text to: {instagramFollowersText.text}");
            }
            
            if (tiktokFollowersText != null)
            {
                tiktokFollowersText.text = socialMetrics.TikTokFollowers.ToString("N0");
                Debug.Log($"[UIManager] Set TikTok followers text to: {tiktokFollowersText.text}");
            }
            
            if (tiktokLikesText != null)
            {
                tiktokLikesText.text = socialMetrics.TikTokLikes.ToString("N0");
                Debug.Log($"[UIManager] Set TikTok likes text to: {tiktokLikesText.text}");
            }
            
            // Update last update timestamp
            if (lastUpdateText != null)
                lastUpdateText.text = "Last updated: " + socialMetrics.Timestamp.ToString("g");
        }
        else
        {
            Debug.LogWarning($"[UIManager] No social media metrics found for {bgsnlCity.Name} (ID: {cityId})");
            
            // Set default values
            if (instagramFollowersText != null) instagramFollowersText.text = "0";
            if (tiktokFollowersText != null) tiktokFollowersText.text = "0";
            if (tiktokLikesText != null) tiktokLikesText.text = "0";
        }
        
        // Update event metrics
        if (eventMetrics != null)
        {
            Debug.Log($"[UIManager] Found event metrics: Tickets={eventMetrics.TicketsSold}, Attendance={eventMetrics.AverageAttendance}, Events={eventMetrics.NumberOfEvents}");
            
            if (ticketsSoldText != null)
            {
                ticketsSoldText.text = eventMetrics.TicketsSold.ToString("N0");
                Debug.Log($"[UIManager] Set tickets sold text to: {ticketsSoldText.text}");
            }
            
            if (averageAttendanceText != null)
            {
                averageAttendanceText.text = eventMetrics.AverageAttendance.ToString("N1");
                Debug.Log($"[UIManager] Set average attendance text to: {averageAttendanceText.text}");
            }
            
            if (numberOfEventsText != null)
            {
                numberOfEventsText.text = eventMetrics.NumberOfEvents.ToString();
                Debug.Log($"[UIManager] Set number of events text to: {numberOfEventsText.text}");
            }
            
            // If social media timestamp wasn't available, use event timestamp for last update
            if (lastUpdateText != null && lastUpdateText.text == "Last updated: ")
                lastUpdateText.text = "Last updated: " + eventMetrics.Timestamp.ToString("g");
        }
        else
        {
            Debug.LogWarning($"[UIManager] No event metrics found for {bgsnlCity.Name} (ID: {cityId})");
            
            // Set default values
            if (ticketsSoldText != null) ticketsSoldText.text = "0";
            if (averageAttendanceText != null) averageAttendanceText.text = "0";
            if (numberOfEventsText != null) numberOfEventsText.text = "0";
        }
        
        // If no data was found at all
        if (socialMetrics == null && eventMetrics == null)
        {
            if (lastUpdateText != null)
                lastUpdateText.text = "No data available";
        }
        
        // Mark as updated to prevent multiple updates
        hasUpdatedUI = true;
        Debug.Log("[UIManager] Dashboard update complete. UI is now locked against further automatic updates.");
    }
} 
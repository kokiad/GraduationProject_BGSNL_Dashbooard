using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleSheetsService : MonoBehaviour
{
    [Header("Google Sheets API Configuration")]
    [SerializeField] private string apiKey;
    [SerializeField] private string spreadsheetId;
    [SerializeField] private string socialMediaSheetName = "SocialMedia";
    [SerializeField] private string eventsSheetName = "Events";
    
    [Header("Cache Settings")]
    [SerializeField] private string cacheDirectory = "GoogleSheetsCache";
    [SerializeField] private float cacheDurationHours = 24f;
    
    [Header("References")]
    [SerializeField] private DataModelClasses dataModel;
    
    private const string API_URL_FORMAT = "https://sheets.googleapis.com/v4/spreadsheets/{0}/values/{1}?key={2}";
    private Dictionary<string, DateTime> lastFetchTimes = new Dictionary<string, DateTime>();
    
    private void Awake()
    {
        // Ensure cache directory exists
        string cachePath = Path.Combine(Application.persistentDataPath, cacheDirectory);
        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }
        
        if (dataModel == null)
        {
            dataModel = FindObjectOfType<DataModelClasses>();
            if (dataModel == null)
            {
                Debug.LogError("DataModelClasses reference not set and could not be found in scene!");
            }
        }
    }
    
    private void Start()
    {
        // Try to load cached data first
        LoadCachedData();
        
        // Then refresh from network if needed
        StartCoroutine(RefreshAllData());
    }
    
    /// <summary>
    /// Refreshes all data from Google Sheets
    /// </summary>
    public IEnumerator RefreshAllData()
    {
        yield return FetchSocialMediaData();
        yield return FetchEventData();
        Debug.Log("All data refreshed from Google Sheets");
    }
    
    /// <summary>
    /// Fetches social media metrics data from Google Sheets
    /// </summary>
    public IEnumerator FetchSocialMediaData()
    {
        string sheetRange = $"{socialMediaSheetName}!A1:Z1000"; // Adjust range as needed
        yield return FetchSheetData(sheetRange, ProcessSocialMediaData);
    }
    
    /// <summary>
    /// Fetches event metrics data from Google Sheets
    /// </summary>
    public IEnumerator FetchEventData()
    {
        string sheetRange = $"{eventsSheetName}!A1:Z1000"; // Adjust range as needed
        yield return FetchSheetData(sheetRange, ProcessEventData);
    }
    
    /// <summary>
    /// Generic method to fetch data from a specific sheet and process it
    /// </summary>
    private IEnumerator FetchSheetData(string sheetRange, Action<List<List<string>>> processDataCallback)
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(spreadsheetId))
        {
            Debug.LogError("API Key or Spreadsheet ID not configured!");
            yield break;
        }
        
        // Check if we need to refresh the cache
        bool shouldRefresh = true;
        string cacheKey = sheetRange.Split('!')[0];
        
        if (lastFetchTimes.ContainsKey(cacheKey))
        {
            TimeSpan elapsed = DateTime.Now - lastFetchTimes[cacheKey];
            shouldRefresh = elapsed.TotalHours >= cacheDurationHours;
        }
        
        if (!shouldRefresh)
        {
            Debug.Log($"Using cached data for {cacheKey} (cache still valid)");
            yield break;
        }
        
        string url = string.Format(API_URL_FORMAT, spreadsheetId, sheetRange, apiKey);
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching sheet data: {request.error}");
                
                // Try to use cached data as fallback
                if (TryLoadFromCache(cacheKey, out string cachedJson))
                {
                    Debug.Log($"Using cached data for {cacheKey} as fallback due to network error");
                    try
                    {
                        ProcessJsonResponse(cachedJson, processDataCallback);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing cached data: {ex.Message}");
                    }
                }
                
                yield break;
            }
            
            string jsonResult = request.downloadHandler.text;
            
            // Cache the results
            SaveToCache(cacheKey, jsonResult);
            lastFetchTimes[cacheKey] = DateTime.Now;
            
            // Process the data
            ProcessJsonResponse(jsonResult, processDataCallback);
        }
    }
    
    /// <summary>
    /// Processes the JSON response from Google Sheets API
    /// </summary>
    private void ProcessJsonResponse(string jsonResponse, Action<List<List<string>>> processCallback)
    {
        try
        {
            // Parse the JSON response
            SheetResponse response = JsonUtility.FromJson<SheetResponse>(jsonResponse);
            
            if (response == null || response.values == null || response.values.Count == 0)
            {
                Debug.LogWarning("No data found in sheet response");
                return;
            }
            
            // Pass the parsed data to the appropriate callback
            processCallback(response.values);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing Google Sheets response: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Processes social media metrics data
    /// </summary>
    private void ProcessSocialMediaData(List<List<string>> values)
    {
        if (values == null || values.Count < 2)
        {
            Debug.LogWarning("Not enough data in social media sheet");
            return;
        }
        
        // Get header row for column mapping
        List<string> headers = values[0];
        Dictionary<string, int> columnMap = CreateColumnMap(headers);
        
        // Process data rows
        for (int i = 1; i < values.Count; i++)
        {
            List<string> row = values[i];
            if (row.Count < headers.Count) continue;
            
            try
            {
                Dictionary<string, string> rawData = new Dictionary<string, string>();
                
                // Map column values based on headers
                foreach (var column in columnMap)
                {
                    if (column.Value < row.Count)
                    {
                        rawData[column.Key.ToLower()] = row[column.Value];
                    }
                }
                
                // Extract city ID
                if (!rawData.TryGetValue("city_id", out string cityId) || string.IsNullOrEmpty(cityId))
                {
                    Debug.LogWarning($"Missing city ID in row {i}");
                    continue;
                }
                
                // Find the associated city
                City city = dataModel.GetCityById(cityId);
                if (city == null)
                {
                    Debug.LogWarning($"Unknown city ID: {cityId}");
                    continue;
                }
                
                // Create and populate metrics object
                SocialMediaMetrics metrics = new SocialMediaMetrics(0, 0, 0, city, DateTime.Now);
                metrics.UpdateFromRawData(rawData);
                
                // Add to data model
                dataModel.AddSocialMediaMetrics(metrics);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing social media row {i}: {ex.Message}");
            }
        }
        
        Debug.Log($"Processed {values.Count - 1} social media metrics entries");
    }
    
    /// <summary>
    /// Processes event metrics data
    /// </summary>
    private void ProcessEventData(List<List<string>> values)
    {
        if (values == null || values.Count < 2)
        {
            Debug.LogWarning("Not enough data in events sheet");
            return;
        }
        
        // Get header row for column mapping
        List<string> headers = values[0];
        Dictionary<string, int> columnMap = CreateColumnMap(headers);
        
        // Process data rows
        for (int i = 1; i < values.Count; i++)
        {
            List<string> row = values[i];
            if (row.Count < headers.Count) continue;
            
            try
            {
                Dictionary<string, string> rawData = new Dictionary<string, string>();
                
                // Map column values based on headers
                foreach (var column in columnMap)
                {
                    if (column.Value < row.Count)
                    {
                        rawData[column.Key.ToLower()] = row[column.Value];
                    }
                }
                
                // Extract city ID
                if (!rawData.TryGetValue("city_id", out string cityId) || string.IsNullOrEmpty(cityId))
                {
                    Debug.LogWarning($"Missing city ID in row {i}");
                    continue;
                }
                
                // Find the associated city
                City city = dataModel.GetCityById(cityId);
                if (city == null)
                {
                    Debug.LogWarning($"Unknown city ID: {cityId}");
                    continue;
                }
                
                // Create and populate metrics object
                EventMetrics metrics = new EventMetrics(0, 0, 0, city, DateTime.Now);
                metrics.UpdateFromRawData(rawData);
                
                // Add to data model
                dataModel.AddEventMetrics(metrics);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing event row {i}: {ex.Message}");
            }
        }
        
        Debug.Log($"Processed {values.Count - 1} event metrics entries");
    }
    
    /// <summary>
    /// Creates a mapping of column names to indices
    /// </summary>
    private Dictionary<string, int> CreateColumnMap(List<string> headers)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
        {
            string header = headers[i].Trim().ToLower().Replace(" ", "_");
            map[header] = i;
        }
        return map;
    }
    
    /// <summary>
    /// Saves data to the cache
    /// </summary>
    private void SaveToCache(string key, string jsonData)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, cacheDirectory, $"{key}.json");
            File.WriteAllText(filePath, jsonData, Encoding.UTF8);
            Debug.Log($"Data cached for {key}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving to cache: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Tries to load data from the cache
    /// </summary>
    private bool TryLoadFromCache(string key, out string jsonData)
    {
        jsonData = null;
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, cacheDirectory, $"{key}.json");
            if (File.Exists(filePath))
            {
                jsonData = File.ReadAllText(filePath, Encoding.UTF8);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading from cache: {ex.Message}");
        }
        return false;
    }
    
    /// <summary>
    /// Loads all cached data
    /// </summary>
    private void LoadCachedData()
    {
        try
        {
            string cachePath = Path.Combine(Application.persistentDataPath, cacheDirectory);
            if (!Directory.Exists(cachePath)) return;
            
            string[] files = Directory.GetFiles(cachePath, "*.json");
            
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string jsonData = File.ReadAllText(file, Encoding.UTF8);
                
                if (fileName == socialMediaSheetName)
                {
                    ProcessJsonResponse(jsonData, ProcessSocialMediaData);
                }
                else if (fileName == eventsSheetName)
                {
                    ProcessJsonResponse(jsonData, ProcessEventData);
                }
            }
            
            Debug.Log($"Loaded {files.Length} cached data files");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading cached data: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Forces a refresh of all data
    /// </summary>
    public void ForceRefresh()
    {
        StartCoroutine(RefreshAllData());
    }
}

// Helper classes for JSON deserialization
[Serializable]
public class SheetResponse
{
    public List<List<string>> values;
}

// Setup Instructions (for reference, not included in the code):
// 1. Create a Google Cloud Project: https://console.cloud.google.com/
// 2. Enable Google Sheets API
// 3. Create API Key in Credentials
// 4. Set the API Key in the inspector
// 5. Make your Google Sheet public or share it with appropriate permissions
// 6. Get the Spreadsheet ID from the URL (between /d/ and /edit)
// 7. Set up two sheets: "SocialMedia" and "Events" with appropriate headers
//    - SocialMedia headers: city_id, instagram_followers, tiktok_followers, tiktok_likes, timestamp
//    - Events headers: city_id, tickets_sold, average_attendance, number_of_events, timestamp

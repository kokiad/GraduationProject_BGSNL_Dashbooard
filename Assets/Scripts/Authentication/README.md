# BGSNL Dashboard Authentication System Implementation Guide

## Overview
This guide outlines the implementation of a role-based authentication system for the BGSNL Dashboard app using Google Sign-In and Firebase Authentication.

## System Requirements
- Only authorized board members can access the app
- National board members have full access (all cities)
- City board members have restricted access (only their city)
- Persistent login sessions
- Secure authentication flow

## Implementation Steps

### 1. Firebase Setup
1. **Create Firebase Project**
   - Create new project in Firebase Console
   - Add Android/iOS app configurations
   - Download and integrate google-services.json/GoogleService-Info.plist

2. **Configure Authentication**
   - Enable Google Sign-In method in Firebase Console
   - Set up OAuth 2.0 client IDs
   - Configure authorized domains

3. **Create Firestore Database**
   ```json
   authorized_users: {
     user_id: {
       email: string,
       role: "national" | "city",
       city_id: string,  // "bgsnl" or specific city ID
       name: string,
       board_position: string
     }
   }
   ```

### 2. Unity Implementation

#### A. Scene Setup
1. **Login Scene**
   - Create LoginScene with:
     - Google Sign-In button
     - Loading indicator
     - Error message display
     - App logo and branding

2. **Scene Flow Modification**
   ```
   LoginScene -> HomeScreen (with appropriate city context)
   ```

#### B. Authentication Scripts

1. **AuthenticationManager.cs**
   - Singleton pattern
   - Handle Google Sign-In flow
   - Manage user session
   - Store user role and permissions
   - Interface with Firebase
   - Persist authentication state

2. **UserSession.cs**
   ```csharp
   public class UserSession {
       public string UserId { get; set; }
       public string Email { get; set; }
       public string Role { get; set; }
       public string CityId { get; set; }
       public bool IsNationalBoard => Role == "national";
   }
   ```

3. **AuthenticationEvents.cs**
   - Define events for:
     - Login success/failure
     - Session state changes
     - Logout
     - Permission checks

#### C. UI Modifications

1. **UIManager.cs Updates**
   - Add authentication state awareness
   - Modify dropdown menu based on user role
   - Handle logout functionality
   - Restrict city access based on permissions

2. **Dropdown Menu Prefab**
   - Create two variants:
     - Full menu (national board)
     - Restricted menu (city board)
   - Add logout button to both variants

### 3. Authentication Flow

1. **Initial Launch**
   ```mermaid
   graph TD
      A[App Launch] --> B{Check Session}
      B -->|No Session| C[Login Scene]
      B -->|Valid Session| D[Load Home Screen]
      C --> E[Google Sign-In]
      E --> F{Check Firebase}
      F -->|Authorized| G[Create Session]
      F -->|Unauthorized| H[Show Error]
      G --> D
   ```

2. **Session Management**
   - Store authentication token securely
   - Implement auto-login
   - Handle token refresh
   - Manage logout cleanup

### 4. Permission System

1. **Role-Based Access**
   ```csharp
   public static class Permissions {
       public static bool CanAccessCity(string cityId)
       public static bool CanAccessCitiesScene()
       public static bool IsAuthorizedUser(string email)
   }
   ```

2. **City Access Rules**
   - National board: Access all cities
   - City board: Access only their city
   - Prevent unauthorized scene navigation

### 5. UI/UX Considerations

1. **Login Experience**
   - Smooth transition animations
   - Clear error messages
   - Loading states
   - Offline handling

2. **Menu Adaptations**
   - Dynamic menu item visibility
   - Proper spacing for removed items
   - Consistent styling

### 6. Security Considerations

1. **Data Protection**
   - Secure token storage
   - Email verification
   - Rate limiting
   - Session timeouts

2. **Access Control**
   - Server-side validation
   - Regular permission checks
   - Audit logging

### 7. Testing Plan

1. **Authentication Tests**
   - Login success/failure
   - Session persistence
   - Token refresh
   - Logout flow

2. **Permission Tests**
   - City access restrictions
   - Menu item visibility
   - Scene navigation
   - Invalid session handling

### Implementation Order

1. Firebase project setup
2. Basic authentication flow
3. User session management
4. Permission system
5. UI adaptations
6. Security measures
7. Testing and refinement

### Dependencies
- Firebase Unity SDK
- Google Sign-In for Unity
- Firebase Authentication
- Cloud Firestore

### Notes
- Keep Firebase rules strict and well-defined
- Implement proper error handling
- Consider offline functionality
- Plan for future board member changes
- Document the process for adding new authorized users 
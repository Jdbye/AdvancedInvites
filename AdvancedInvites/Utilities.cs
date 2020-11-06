namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerRuntimeLib.XrefScans;

    using UnityEngine;

    using VRC.Core;
    using VRC.UI;

    using Boolean = Il2CppSystem.Boolean;

    public static class Utilities
    {

        public delegate void SendNotificationDelegate(string receiverUserId, string notificationType, string message, NotificationDetails notificationDetails);

        private static CreatePortalDelegate ourCreatePortalDelegate;

        private static DeleteNotificationDelegate ourDeleteNotificationDelegate;

        private static ShowAlertDelegate ourShowAlertDelegate;

        private static ShowPopupWindowBothDelegate ourShowPopupWindowBothDelegate;

        private static ShowPopupWindowSingleDelegate ourShowPopupWindowSingleDelegate;

        private static VRCUiManagerDelegate ourVRCUiManagerDelegate;

        private static SendNotificationDelegate ourSendNotificationDelegate;

        public static SendNotificationDelegate SendNotification
        {
            get
            {
                if (ourSendNotificationDelegate != null) return ourSendNotificationDelegate;

                // Scan for the method used by invite friend
                MethodInfo inviteFriendMethod = typeof(PageUserInfo).GetMethod(nameof(PageUserInfo.InviteFriend), BindingFlags.Public | BindingFlags.Instance);
                foreach (XrefInstance xrefInstance in XrefScanner.XrefScan(inviteFriendMethod))
                {
                    if (xrefInstance.Type != XrefType.Method) continue;
                    MethodBase resolved = xrefInstance.TryResolve();
                    if (resolved == null) continue;

                    if (string.IsNullOrEmpty(resolved.Name)) continue;
                    if (resolved.ReflectedType == null) continue;
                    if (resolved.ReflectedType != typeof(NotificationManager)) continue;

                    MethodInfo sendNotificationMethod = typeof(NotificationManager).GetMethod(resolved.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (sendNotificationMethod == null) continue;
                    if (!sendNotificationMethod.HasParameters(typeof(string), typeof(string), typeof(string), typeof(NotificationDetails))) continue;

                    ourSendNotificationDelegate = (SendNotificationDelegate)Delegate.CreateDelegate(
                        typeof(SendNotificationDelegate),
                        NotificationManager.prop_NotificationManager_0,
                        sendNotificationMethod);
                    return ourSendNotificationDelegate;
                }

                MelonLogger.LogError("Failed to find SendNotification Method");
                return null;
            }
        }

        internal static VRCUiManagerDelegate GetVRCUiManager
        {
            get
            {
                if (ourVRCUiManagerDelegate != null) return ourVRCUiManagerDelegate;
                MethodInfo vrcUiManagerInstance = typeof(VRCUiManager).GetMethods().First(x => x.ReturnType == typeof(VRCUiManager));
                ourVRCUiManagerDelegate = (VRCUiManagerDelegate)Delegate.CreateDelegate(typeof(VRCUiManagerDelegate), vrcUiManagerInstance);
                return ourVRCUiManagerDelegate;
            }
        }

        private static CreatePortalDelegate GetCreatePortalDelegate
        {
            get
            {
                if (ourCreatePortalDelegate != null) return ourCreatePortalDelegate;
                MethodInfo portalMethod = typeof(PortalInternal).GetMethods(BindingFlags.Public | BindingFlags.Static).First(
                    m => m.ReturnType == typeof(bool) && m.HasParameters(
                             typeof(ApiWorld),
                             typeof(ApiWorldInstance),
                             typeof(Vector3),
                             typeof(Vector3),
                             typeof(bool)) && m.XRefScanFor("admin_dont_allow_portal"));
                ourCreatePortalDelegate = (CreatePortalDelegate)Delegate.CreateDelegate(typeof(CreatePortalDelegate), portalMethod);
                return ourCreatePortalDelegate;
            }
        }

        private static DeleteNotificationDelegate GetDeleteNotificationDelegate
        {
            get
            {
                if (ourDeleteNotificationDelegate != null) return ourDeleteNotificationDelegate;

                MethodInfo deleteMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => m.XRefScanFor("voteToKick") && m.XRefScanForMethod(null, nameof(VRCWebSocketsManager))
                                                     && m.XRefScanMethodCount(null, nameof(NotificationManager)) == 2);
                ourDeleteNotificationDelegate = (DeleteNotificationDelegate)Delegate.CreateDelegate(
                    typeof(DeleteNotificationDelegate),
                    NotificationManager.field_Private_Static_NotificationManager_0,
                    deleteMethod);
                return ourDeleteNotificationDelegate;
            }
        }

        private static ShowAlertDelegate GetShowAlertDelegate
        {
            get
            {
                if (ourShowAlertDelegate != null) return ourShowAlertDelegate;
                MethodInfo alertMethod = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                                  .First(m => m.GetParameters().Length == 3 && m.XRefScanFor("Popups/AlertPopup"));
                ourShowAlertDelegate = (ShowAlertDelegate)Delegate.CreateDelegate(
                    typeof(ShowAlertDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    alertMethod);
                return ourShowAlertDelegate;
            }
        }

        private static ShowPopupWindowBothDelegate GetShowPopupWindowBothDelegate
        {
            get
            {
                if (ourShowPopupWindowBothDelegate != null) return ourShowPopupWindowBothDelegate;
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.GetParameters().Length == 7 && m.XRefScanFor("Popups/StandardPopupV2"));

                ourShowPopupWindowBothDelegate = (ShowPopupWindowBothDelegate)Delegate.CreateDelegate(
                    typeof(ShowPopupWindowBothDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    popupV2Method);
                return ourShowPopupWindowBothDelegate;
            }
        }

        private static ShowPopupWindowSingleDelegate GetShowPopupWindowSingleDelegate
        {
            get
            {
                if (ourShowPopupWindowSingleDelegate != null) return ourShowPopupWindowSingleDelegate;
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.GetParameters().Length == 5 && m.XRefScanFor("Popups/StandardPopupV2"));

                ourShowPopupWindowSingleDelegate = (ShowPopupWindowSingleDelegate)Delegate.CreateDelegate(
                    typeof(ShowPopupWindowSingleDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    popupV2Method);
                return ourShowPopupWindowSingleDelegate;
            }
        }

        public static ApiWorldInstance.AccessType GetAccessType(string tags)
        {
            if (tags.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0) return ApiWorldInstance.AccessType.FriendsOfGuests;
            if (tags.IndexOf("friends", StringComparison.OrdinalIgnoreCase) >= 0) return ApiWorldInstance.AccessType.FriendsOnly;
            if (tags.IndexOf("request", StringComparison.OrdinalIgnoreCase) >= 0) return ApiWorldInstance.AccessType.InvitePlus;
            return tags.IndexOf("private", StringComparison.OrdinalIgnoreCase) >= 0
                       ? ApiWorldInstance.AccessType.InviteOnly
                       : ApiWorldInstance.AccessType.Public;
        }

        public static string GetAccessName(ApiWorldInstance.AccessType accessType)
        {
            // Switch expression. yes c# 8 works in MelonLoader as it compiles differently
            return accessType switch
                {
                    ApiWorldInstance.AccessType.Public => "Public",
                    ApiWorldInstance.AccessType.FriendsOfGuests => "Friends+",
                    ApiWorldInstance.AccessType.FriendsOnly => "Friends Only",
                    ApiWorldInstance.AccessType.InviteOnly => "Invite Only",
                    ApiWorldInstance.AccessType.InvitePlus => "Invite+",
                    ApiWorldInstance.AccessType.Counter => "Coun... wait wut?",
                    _ => throw new ArgumentOutOfRangeException(nameof(accessType), accessType, "what the fuck happened?")
                };
        }
        
        // Don't ask alright. either i'm too tired or this is too weird.
        // Taken directly from older vrchat source
        public static bool IsPlatformCompatibleWithCurrentWorld(string platform)
        {
            if (RoomManager.field_Internal_Static_ApiWorld_0 == null)
            {
                return false;
            }
            bool notUsingAndroid = !string.IsNullOrEmpty(platform) && platform.Contains("android");
            if (RoomManager.field_Internal_Static_ApiWorld_0.supportedPlatforms == ApiModel.SupportedPlatforms.Android && !notUsingAndroid) return false;
            return RoomManager.field_Internal_Static_ApiWorld_0.supportedPlatforms != ApiModel.SupportedPlatforms.StandaloneWindows || !notUsingAndroid;
        }

        public static void AcceptInviteRequest(string receiverUserId)
        {
            ApiWorld currentRoom = RoomManager.field_Internal_Static_ApiWorld_0;
            NotificationDetails details = new NotificationDetails
                                              {
                                                  ["worldId"] = $"{currentRoom.id}:{currentRoom.currentInstanceIdWithTags}",
                                                  ["rsvp"] = new Boolean { m_value = true }.BoxIl2CppObject(),
                                                  ["worldName"] = currentRoom.name
                                              };

            SendNotification(receiverUserId, "invite", string.Empty, details);
        }

        public static void SendIncompatiblePlatformNotification(string receiverUserId)
        {
            NotificationDetails details = new NotificationDetails
                                              {
                                                  ["incompatible"] = new Boolean { m_value = true }.BoxIl2CppObject(),
                                                  ["rsvp"] = new Boolean { m_value = true }.BoxIl2CppObject()
                                              };

            SendNotification(receiverUserId, "invite", string.Empty, details);
        }

        public static bool AllowedToInvite()
        {
            if (RoomManager.field_Internal_Static_ApiWorldInstance_0.GetInstanceCreator().Equals(APIUser.CurrentUser.id, StringComparison.Ordinal)) return true;
            return GetAccessType(RoomManager.field_Internal_Static_ApiWorld_0.currentInstanceIdWithTags) switch
                {
                    ApiWorldInstance.AccessType.Public          => true,
                    ApiWorldInstance.AccessType.FriendsOfGuests => true,
                    ApiWorldInstance.AccessType.InvitePlus      => true,

                    // Not instance owner so no
                    ApiWorldInstance.AccessType.FriendsOnly => false,
                    ApiWorldInstance.AccessType.InviteOnly  => false,
                    _                                       => true
                };
        }

        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts)
        {
            return GetCreatePortalDelegate(apiWorld, apiWorldInstance, position, forward, showAlerts);
        }

        public static void DeleteNotification(Notification notification)
        {
            GetDeleteNotificationDelegate(notification);
        }

        public static Notification GetCurrentActiveNotification()
        {
            return QuickMenu.prop_QuickMenu_0.field_Private_Notification_0;
        }

        public static Transform GetLocalPlayerTransform()
        {
            return VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
        }

        public static void HideCurrentPopup()
        {
            GetVRCUiManager().HideScreen("POPUP");
        }
        public static void ShowAlert(string title, string content, float timeOut = 10f)
        {
            GetShowAlertDelegate(title, content, timeOut);
        }

        public static void ShowPopupWindow(
            string title,
            string content,
            string button1,
            Action action,
            string button2,
            Action action2,
            Action<VRCUiPopup> onCreated = null)
        {
            GetShowPopupWindowBothDelegate(title, content, button1, action, button2, action2, onCreated);
        }

        public static void ShowPopupWindow(string title, string content, string button1, Action action, Action<VRCUiPopup> onCreated = null)
        {
            GetShowPopupWindowSingleDelegate(title, content, button1, action, onCreated);
        }

        public static bool XRefScanFor(this MethodBase methodBase, string searchTerm)
        {
            return XrefScanner.XrefScan(methodBase).Any(
                xref => xref.Type == XrefType.Global && xref.ReadAsObject()?.ToString().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool HasParameters(this MethodBase methodBase, params Type[] types)
        {
            ParameterInfo[] parameters = methodBase.GetParameters();
            int typesLength = types.Length;
            if (parameters.Length < typesLength) return false;

            for (var i = 0; i < typesLength; ++i)
                if (parameters[i].ParameterType != types[i])
                    return false;

            return true;
        }

        private static bool XRefScanForMethod(this MethodBase methodBase, string methodName = null, string parentType = null, bool ignoreCase = true)
        {
            if (!string.IsNullOrEmpty(methodName)
                || !string.IsNullOrEmpty(parentType))
                return XrefScanner.XrefScan(methodBase).Any(
                    xref =>
                        {
                            if (xref.Type != XrefType.Method) return false;

                            var found = false;
                            MethodBase resolved = xref.TryResolve();
                            if (resolved == null) return false;

                            if (!string.IsNullOrEmpty(methodName))
                                found = !string.IsNullOrEmpty(resolved.Name) && resolved.Name.IndexOf(
                                            methodName,
                                            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;

                            if (!string.IsNullOrEmpty(parentType))
                                found = !string.IsNullOrEmpty(resolved.ReflectedType?.Name) && resolved.ReflectedType.Name.IndexOf(
                                            parentType,
                                            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;

                            return found;
                        });
            MelonLogger.LogWarning($"XRefScanForMethod \"{methodBase}\" has all null/empty parameters. Returning false");
            return false;
        }

        private static int XRefScanMethodCount(this MethodBase methodBase, string methodName = null, string parentType = null, bool ignoreCase = true)
        {
            if (!string.IsNullOrEmpty(methodName)
                || !string.IsNullOrEmpty(parentType))
                return XrefScanner.XrefScan(methodBase).Count(
                    xref =>
                        {
                            if (xref.Type != XrefType.Method) return false;

                            var found = false;
                            MethodBase resolved = xref.TryResolve();
                            if (resolved == null) return false;

                            if (!string.IsNullOrEmpty(methodName))
                                found = !string.IsNullOrEmpty(resolved.Name) && resolved.Name.IndexOf(
                                            methodName,
                                            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;

                            if (!string.IsNullOrEmpty(parentType))
                                found = !string.IsNullOrEmpty(resolved.ReflectedType?.Name) && resolved.ReflectedType.Name.IndexOf(
                                            parentType,
                                            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;

                            return found;
                        });
            MelonLogger.LogWarning($"XRefScanMethodCount \"{methodBase}\" has all null/empty parameters. Returning -1");
            return -1;
        }

        internal delegate VRCUiManager VRCUiManagerDelegate();

        private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts);

        private delegate void DeleteNotificationDelegate(Notification notification);

        private delegate void ShowAlertDelegate(string title, string content, float timeOut);

        private delegate void ShowPopupWindowBothDelegate(
            string title,
            string content,
            string button1,
            Il2CppSystem.Action action,
            string button2,
            Il2CppSystem.Action action2,
            Il2CppSystem.Action<VRCUiPopup> onCreated = null);

        private delegate void ShowPopupWindowSingleDelegate(
            string title,
            string content,
            string button,
            Il2CppSystem.Action action,
            Il2CppSystem.Action<VRCUiPopup> onCreated = null);

    }

}
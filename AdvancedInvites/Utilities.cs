namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerBaseLib;

    using UnhollowerRuntimeLib.XrefScans;

    using UnityEngine;

    using VRC;
    using VRC.Core;
    using VRC.UI;

    using Boolean = Il2CppSystem.Boolean;

    public static class Utilities
    {

        public enum InstanceRegion
        {

            US,

            EU,

            JP

        }

        public static InstanceRegion GetInstanceRegion(string tags)
        {
            if (tags.IndexOf("region(eu)", StringComparison.OrdinalIgnoreCase) != -1) return InstanceRegion.EU;
            return tags.IndexOf("region(jp)", StringComparison.OrdinalIgnoreCase) != -1 ? InstanceRegion.JP : InstanceRegion.US;
        }

        public static string RegionToName(InstanceRegion region)
        {
            return region switch
                {
                    InstanceRegion.US => "USA",
                    InstanceRegion.EU => "Europe",
                    InstanceRegion.JP => "Japan",
                    _                 => throw new ArgumentOutOfRangeException(nameof(region), region, null)
                };
        }

        //public delegate bool StreamerModeDelegate();

        public delegate VRCUiManager VRCUiManagerDelegate();

        private static CreatePortalDelegate ourCreatePortalDelegate;

        private static DeleteNotificationDelegate ourDeleteNotificationDelegate;

        private static ShowAlertDelegate ourShowAlertDelegate;

        private static ShowPopupWindowBothDelegate ourShowPopupWindowBothDelegate;

        private static ShowPopupWindowSingleDelegate ourShowPopupWindowSingleDelegate;

        private static VRCUiManagerDelegate ourVRCUiManagerDelegate;

        private static SendNotificationDelegate ourSendNotificationDelegate;

        /* public static StreamerModeDelegate GetStreamerMode
         {
             get
             {
                 if (ourStreamerModeDelegate != null) return ourStreamerModeDelegate;
 
                 PropertyInfo streamerModeProperty = typeof(VRCInputManager).GetProperties(BindingFlags.Public | BindingFlags.Static).First(
                     property => property.PropertyType == typeof(bool)
                                 && XrefScanner.XrefScan(property.GetSetMethod()).Any(
                                     xref => xref.Type == XrefType.Global && xref.ReadAsObject()?.ToString().IndexOf("STREAMER", StringComparison.OrdinalIgnoreCase) != -1));
 
                 ourStreamerModeDelegate = (StreamerModeDelegate)Delegate.CreateDelegate(typeof(StreamerModeDelegate), streamerModeProperty.GetGetMethod());
                 return ourStreamerModeDelegate;
             }
         }*/

        private static SendNotificationDelegate SendNotification
        {
            get
            {
                if (ourSendNotificationDelegate != null) return ourSendNotificationDelegate;

                foreach (MethodInfo method in typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (method.IsAbstract
                        || method.IsVirtual) continue;
                    if (!method.HasParameters(
                            typeof(string),
                            typeof(string),
                            typeof(string),
                            typeof(string),
                            typeof(NotificationDetails),
                            typeof(Il2CppStructArray<byte>))) continue;

                    if (!XrefScanner.UsedBy(method).Any(
                            instance => instance.Type == XrefType.Method
                                        && instance.TryResolve()?.ReflectedType?.Equals(typeof(PageUserInfo)) == true)) continue;

                    ourSendNotificationDelegate = (SendNotificationDelegate)Delegate.CreateDelegate(
                        typeof(SendNotificationDelegate),
                        NotificationManager.prop_NotificationManager_0,
                        method);
                    return ourSendNotificationDelegate;
                }

                MelonLogger.Error("Failed to find SendNotification Method");
                return null;
            }
        }

        public static VRCUiManagerDelegate GetVRCUiManager
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
                MethodInfo portalMethod = typeof(PortalInternal).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).First(
                    m => m.ReturnType == typeof(bool)
                         && m.HasParameters(typeof(ApiWorld), typeof(ApiWorldInstance), typeof(Vector3), typeof(Vector3))
                         && m.XRefScanFor("admin_dont_allow_portal"));
                ourCreatePortalDelegate = (CreatePortalDelegate)Delegate.CreateDelegate(typeof(CreatePortalDelegate), portalMethod);
                return ourCreatePortalDelegate;
            }
        }

        private static DeleteNotificationDelegate GetDeleteNotificationDelegate
        {
            get
            {
                if (ourDeleteNotificationDelegate != null) return ourDeleteNotificationDelegate;

                // Appears to be NotificationManager.Method_Public_Void_Notification_1(notification); 
                foreach (MethodInfo method in typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    // First some pre-filtering before x-referencing it
                    if (!method.Name.StartsWith("Method_Public_Void_")) continue;
                    if (method.GetParameters().Length != 1) continue;
                    if (method.GetParameters()[0].ParameterType != typeof(Notification)) continue;

                    // So far it's mostly always had voteToKick in it for special case
                    if (!method.XRefScanFor("voteToKick")) continue;

                    // Notification Manager count seems to be at least 3 of
                    if (method.XRefScanMethodCount(null, nameof(MonoBehaviourPublicObApAcApStAcBoStBoObUnique)) < 1
                        || method.XRefScanMethodCount(null, nameof(NotificationManager)) < 3) continue;

                    // The real one is used by the quick menu and itself 
                    if (!XrefScanner.UsedBy(method).Any(
                            instance => instance.Type == XrefType.Method && instance.TryResolve()?.DeclaringType == typeof(NotificationManager))) continue;
                    if (!XrefScanner.UsedBy(method).Any(
                            instance => instance.Type == XrefType.Method && instance.TryResolve()?.DeclaringType == typeof(Object1Public1TeBo1TeStOb1DaStUnique)))
                        continue;

                    // Well seems to be the right one, let's grab it
                    ourDeleteNotificationDelegate = (DeleteNotificationDelegate)Delegate.CreateDelegate(
                        typeof(DeleteNotificationDelegate),
                        NotificationManager.field_Private_Static_NotificationManager_0,
                        method);
                    return ourDeleteNotificationDelegate;
                }

                return null;
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
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => m.Name.IndexOf("pdm", StringComparison.OrdinalIgnoreCase) == -1
                         && m.GetParameters().Length == 7
                         && m.XRefScanFor("Popups/StandardPopupV2"));

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
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => m.GetParameters().Length == 5
                         && m.Name.IndexOf("pdm", StringComparison.OrdinalIgnoreCase) == -1
                         && m.XRefScanFor("Popups/StandardPopupV2")

                         // Loukylor told me about this
                         && XrefScanner.UsedBy(m).Any(
                             xref => xref.Type == XrefType.Method
                                     && xref.TryResolve()?.Name.IndexOf("OpenSaveSearchPopup", StringComparison.OrdinalIgnoreCase) != -1));

                ourShowPopupWindowSingleDelegate = (ShowPopupWindowSingleDelegate)Delegate.CreateDelegate(
                    typeof(ShowPopupWindowSingleDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    popupV2Method);
                return ourShowPopupWindowSingleDelegate;
            }
        }

        //private static StreamerModeDelegate ourStreamerModeDelegate;

        public static bool GetStreamerMode()
        {
            return VRCInputManager.Method_Public_Static_Boolean_InputSetting_0(
                VRCInputManager.InputSetting.StreamerModeEnabled);
        }

    #if DEBUG
        public static void Request()
        {
            NotificationDetails details = new NotificationDetails();
            details.Add("platform", Tools.Platform);

            SendNotification(APIUser.CurrentUser.displayName, APIUser.CurrentUser.id, "requestInvite", string.Empty, details);
        }
    #endif

        public static ApiWorld CurrentRoom()
        {
            return RoomManager.field_Internal_Static_ApiWorld_0;
        }

        public static ApiWorldInstance CurrentWorldInstance()
        {
            return RoomManager.field_Internal_Static_ApiWorldInstance_0;
        }

        public static void QueueHudMessage(string msg)
        {
            GetVRCUiManager().field_Private_List_1_String_0.Add(msg);
        }

        public static InstanceAccessType GetAccessType(string tags)
        {
            if (tags.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0) return InstanceAccessType.FriendsOfGuests;
            if (tags.IndexOf("friends", StringComparison.OrdinalIgnoreCase) >= 0) return InstanceAccessType.FriendsOnly;
            if (tags.IndexOf("request", StringComparison.OrdinalIgnoreCase) >= 0) return InstanceAccessType.InvitePlus;
            return tags.IndexOf("private", StringComparison.OrdinalIgnoreCase) >= 0 ? InstanceAccessType.InviteOnly : InstanceAccessType.Public;
        }

        public static string GetAccessName(InstanceAccessType accessType)
        {
            return accessType switch
                {
                    InstanceAccessType.Public          => "Public",
                    InstanceAccessType.FriendsOfGuests => "Friends+",
                    InstanceAccessType.FriendsOnly     => "Friends Only",
                    InstanceAccessType.InviteOnly      => "Invite Only",
                    InstanceAccessType.InvitePlus      => "Invite+",
                    _                                  => throw new ArgumentOutOfRangeException(nameof(accessType), accessType, "what the fuck happened?")
                };
        }

        public static bool IsPlatformCompatibleWithCurrentWorld(string platform)
        {
            if (CurrentRoom() == null) return false;
            if (string.IsNullOrEmpty(platform)) return false; // true or false? supposed to be included at least

            return CurrentRoom().supportedPlatforms switch
                {
                    ApiModel.SupportedPlatforms.StandaloneWindows => Tools.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase),
                    ApiModel.SupportedPlatforms.Android           => Tools.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase),
                    ApiModel.SupportedPlatforms.All               => true,
                    _                                             => true
                };
        }

        public static void AcceptInviteRequest(string receiverUserId, string receiverUserName)
        {
            ApiWorld currentRoom = CurrentRoom();
            NotificationDetails details = new NotificationDetails();
            details.Add("worldId", $"{currentRoom.id}:{CurrentInstanceCached.InstanceId}");

            // don't ask me why, ask vrchat why they added instanceId as
            // a direct copy of worldId with both having both world and instance id
            details.Add("instanceId", $"{currentRoom.id}:{CurrentInstanceCached.InstanceId}");

            //details.Add("rsvp", new Boolean { m_value = true }.BoxIl2CppObject()); // Doesn't work for some reason
            details.Add("worldName", currentRoom.name);

            SendNotification(receiverUserName, receiverUserId, "invite", string.Empty, details);
        }

        // Since stuff bugs out if you do boxed booleans this will remain here unused till i might figure out something (or someone else does)
        public static void SendIncompatiblePlatformNotification(string receiverUserId, string receiverUserName)
        {
            NotificationDetails details = new NotificationDetails();
            details.Add("incompatible", new Boolean { m_value = true }.BoxIl2CppObject());
            details.Add("rsvp", new Boolean { m_value = true }.BoxIl2CppObject());

            SendNotification(receiverUserName, receiverUserId, "invite", string.Empty, details);
        }

        public static bool AllowedToInvite()
        {
            // Instance owner
            if (CurrentInstanceCached.OwnerId?.Equals(APIUser.CurrentUser.id, StringComparison.Ordinal) == true) return true;
            return CurrentInstanceCached.AccessType switch
                {
                    InstanceAccessType.Public          => true,
                    InstanceAccessType.FriendsOfGuests => true,
                    InstanceAccessType.InvitePlus      => true,

                    // Not instance owner/not mutual friend so no
                    InstanceAccessType.FriendsOnly => false,
                    InstanceAccessType.InviteOnly  => false,
                    _                              => false
                };
        }

        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, Action<string> errorReasonCallback)
        {
            return GetCreatePortalDelegate(apiWorld, apiWorldInstance, position, forward, errorReasonCallback);
        }

        public static void ShowErrorReason(string error)
        {
            Utilities.ShowAlert("Error Creating Portal", error);
        }

        internal static IntPtr GetFunctionPointer(string methodName)
        {
            return typeof(Utilities).GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Static)!.MethodHandle.GetFunctionPointer();
        }

        public static void DeleteNotification(Notification notification)
        {
            try
            {
                GetDeleteNotificationDelegate(notification);
            }
            catch (Exception e)
            {
                MelonLogger.Warning(
                    "Couldn't find the delete notification method. it'll still work, just delete the notification yourself after or let it get auto-deleted by vrchat itself");
                MelonLogger.Error(e);
            }
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
                            {
                                found = !string.IsNullOrEmpty(resolved.Name)
                                        && resolved.Name.IndexOf(methodName, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;
                                if (!found) return false;
                            }

                            if (!string.IsNullOrEmpty(parentType))
                                found = !string.IsNullOrEmpty(resolved.ReflectedType?.Name)
                                        && resolved.ReflectedType.Name.IndexOf(
                                            parentType,
                                            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                                        >= 0;

                            return found;
                        });
            MelonLogger.Warning($"XRefScanForMethod \"{methodBase}\" has all null/empty parameters. Returning false");
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
                            {
                                found = !string.IsNullOrEmpty(resolved.Name)
                                        && resolved.Name.IndexOf(methodName, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;
                                if (!found) return false;
                            }

                            if (!string.IsNullOrEmpty(parentType))
                                found = !string.IsNullOrEmpty(resolved.ReflectedType?.Name)
                                        && resolved.ReflectedType.Name.IndexOf(
                                            parentType,
                                            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                                        >= 0;

                            return found;
                        });
            MelonLogger.Warning($"XRefScanMethodCount \"{methodBase}\" has all null/empty parameters. Returning -1");
            return -1;
        }

        internal readonly struct WorldInstanceCache
        {

            public string InstanceId { get; }

            public string OwnerId { get; }

            public InstanceAccessType AccessType { get; }

            public WorldInstanceCache(ApiWorldInstance instance)
            {
                InstanceId = instance.instanceId;
                OwnerId = instance.ownerId;
                AccessType = instance.type;
            }

        }

        internal static WorldInstanceCache CurrentInstanceCached;

        private delegate void SendNotificationDelegate(
            string receiverUserName,
            string receiverUserId,
            string notificationType,
            string message,
            NotificationDetails notificationDetails,
            Il2CppStructArray<byte> picDataIGuess = null);

        private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, Il2CppSystem.Action<string> errorReasonCallback = null);

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
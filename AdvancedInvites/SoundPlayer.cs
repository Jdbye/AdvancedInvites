namespace AdvancedInvites
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using MelonLoader;

    using UnityEngine;

    using File = System.IO.File;
    using FileMode = System.IO.FileMode;
    using FileStream = System.IO.FileStream;
    using Path = System.IO.Path;
    using Stream = System.IO.Stream;

    public class SoundPlayer
    {

        public enum NotificationType
        {

            Default,

            Invite,

            InviteRequest,

            FriendRequest,

            VoteToKick

        }

        private const string AudioResourceFolder = "UserData/AdvancedInvites/";

        private static SoundPlayer instance;

        public static float Volume;

        private Dictionary<NotificationType, AudioClip> audioClipDictionary;

        private GameObject audioGameObject;

        private AudioSource audioSource;

        private SoundPlayer()
        { }

        private static string GetAudioPath(NotificationType notificationType)
        {
            return Path.GetFullPath(Path.Combine(AudioResourceFolder, $"{notificationType}.ogg"));
        }

        public static void PlayNotificationSound(NotificationType notificationType)
        {
            if (instance == null
                || instance.audioSource == null) return;

            instance.audioSource.outputAudioMixerGroup = null;

            if (notificationType != NotificationType.Default
                && instance.audioClipDictionary.ContainsKey(notificationType)
                && instance.audioClipDictionary[notificationType].loadState == AudioDataLoadState.Loaded)
                instance.audioSource.PlayOneShot(instance.audioClipDictionary[notificationType], Volume);
            else if (instance.audioClipDictionary.ContainsKey(NotificationType.Default)
                     && instance.audioClipDictionary[NotificationType.Default].loadState == AudioDataLoadState.Loaded)
                instance.audioSource.PlayOneShot(instance.audioClipDictionary[NotificationType.Default], Volume);
        }

        private static IEnumerator LoadNotificationSounds()
        {
            MelonLogger.Msg("Loading Notification Sound(s)");
            instance.audioClipDictionary = new Dictionary<NotificationType, AudioClip>();

            // Legacy Convert
            if (File.Exists(Path.GetFullPath(Path.Combine(AudioResourceFolder, "Notification.ogg"))))
            {
                MelonLogger.Msg("Found old notification file. renaming to Default.ogg");
                File.Move(Path.GetFullPath(Path.Combine(AudioResourceFolder, "Notification.ogg")), GetAudioPath(NotificationType.Default));
            }

            if (!File.Exists(GetAudioPath(NotificationType.Default)))
            {
                MelonLogger.Msg("Default Notification sound not found. Creating default one");
                try
                {
                    using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdvancedInvites.Notification.ogg");
                    if (stream != null)
                    {
                        using FileStream fs = new FileStream(GetAudioPath(NotificationType.Default), FileMode.Create);
                        stream.CopyTo(fs);
                        fs.Close();
                        stream.Close();
                    }
                    else
                    {
                        MelonLogger.Error("Failed to open Resource Stream for Notification.ogg");
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Something went wrong writing the default notification file to UserData/AdvancedInvites Folder:\n" + e);
                    yield break;
                }
            }

            foreach (string name in Enum.GetNames(typeof(NotificationType)).Where(
                name => File.Exists(GetAudioPath((NotificationType)Enum.Parse(typeof(NotificationType), name)))))
                yield return LoadAudioClip((NotificationType)Enum.Parse(typeof(NotificationType), name));
        }

        private static IEnumerator LoadAudioClip(NotificationType notificationType)
        {
            WWW request = new WWW(GetAudioPath(notificationType), null, new Il2CppSystem.Collections.Generic.Dictionary<string, string>());
            AudioClip audioClip = request.GetAudioClip(false, false, AudioType.OGGVORBIS);

            while (!request.isDone || audioClip.loadState == AudioDataLoadState.Loading) yield return new WaitForEndOfFrame();
            request.Dispose();

            if (audioClip.loadState == AudioDataLoadState.Loaded)
            {
                instance.audioClipDictionary.Add(notificationType, audioClip);
                instance.audioClipDictionary[notificationType].hideFlags = HideFlags.HideAndDontSave;
                MelonLogger.Msg($"{notificationType} Notification Sound Loaded");
            }
            else if (audioClip.loadState == AudioDataLoadState.Failed)
            {
                MelonLogger.Error($"Failed To Load {notificationType} Notification Sound");
            }
        }


        public static void Initialize()
        {
            if (instance != null) return;

            instance = new SoundPlayer();
            instance.audioGameObject = new GameObject { hideFlags = HideFlags.HideAndDontSave };
            instance.audioSource = instance.audioGameObject.AddComponent<AudioSource>();
            instance.audioSource.hideFlags = HideFlags.HideAndDontSave;
            instance.audioSource.dopplerLevel = 0f;
            instance.audioSource.spatialBlend = 0f;
            instance.audioSource.spatialize = false;

            MelonCoroutines.Start(LoadNotificationSounds());
        }

    }

}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phantom.Core;
using Phantom.Utils;


using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;

namespace Phantom.Audio
{
    public class Sound
    {
        public static float MasterVolume = 1f;
        public static float FXVolume = 1f;

        public static Audio.Handle Play(string sound, float volume = -1f, float panning = 0f)
        {
#if !NOAUDIO
            sound = sound.Trim().ToLower();

            var info = Audio.Instance.audiolist[sound];

            // Check if this sound is limited:
            if (info.Limit >= 0 && (info.Limit == 0 || (Audio.Instance.soundLimits.ContainsKey(sound) && Audio.Instance.soundLimits[sound] >= info.Limit)))
            {
                Debug.WriteLine("Audio limit reached: " + info.Name + " (limit: " + info.Limit + ")");
                return default(Audio.Handle);
            }

            // Don't play if volume is off:
            volume = Audio.Volume(volume, info) * MasterVolume * FXVolume;
            if (volume <= 0)
                return default(Audio.Handle);

            // Load and create instance: (loading is cached)
            var effect = Audio.Instance.Load(info.Asset);
            var instance = effect.CreateInstance();

            instance.Volume = volume;
            instance.Pan = panning;
            instance.IsLooped = false;
            instance.Play();

            var handle = new Audio.Handle
            {
                Success = true,
                Instance = instance,
                Name = info.Name,
                Type = Audio.Type.Sound,
				Looped = false
            };
            Audio.Instance.AddHandle(handle);
            return handle;
#else
            return default(Audio.Handle);
#endif
        }

        public static Audio.Handle FadeIn(string sound, bool looped, float duration, TweenFunction function = null, float volume = -1)
        {
#if !NOAUDIO
            sound = sound.Trim().ToLower();
            if (function == null)
                function = TweenFunctions.Linear;
            
            var info = Audio.Instance.audiolist[sound];
            var handle = Play(sound, 0.00001f);
            if (!handle.Success)
                return handle;

            handle.Instance.IsLooped = looped;

            handle.FadeState = 1;
            handle.FadeDuration = handle.FadeTimer = duration;
            handle.FadeFunction = function;
            handle.FadeVolume = Audio.Volume(volume, info);

            return handle;
#else
            return default(Audio.Handle);
#endif
        }

        public static void FadeOut(Audio.Handle handle, float duration, TweenFunction function = null)
        {
#if !NOAUDIO
            if (function == null)
                function = TweenFunctions.Linear;
            handle.FadeState = -1;
            handle.FadeDuration = handle.FadeTimer = duration;
            handle.FadeFunction = function;
            handle.FadeVolume = handle.Instance.Volume;
#endif
        }

        public static void Stop(string sound)
        {
#if !NOAUDIO
            if (!Audio.Instance.handlesMap.ContainsKey(sound))
                return;
            for (int i = Audio.Instance.handlesMap[sound].Count - 1; i >= 0; --i)
                Stop(Audio.Instance.handlesMap[sound][i]);
#endif
        }

        public static void Stop(Audio.Handle sound)
        {
#if !NOAUDIO
            sound.Instance.IsLooped = false;
            sound.Instance.Stop();
#endif
        }


        public static void StopAll()
        {
            for (int i = Audio.Instance.handles.Count - 1; i >= 0; --i)
                if(Audio.Instance.handles[i].Type == Audio.Type.Sound)
                    Sound.Stop(Audio.Instance.handles[i]);
        }
        public static void FadeOutAll(float duration, TweenFunction function = null)
        {
            if (function == null)
                function = TweenFunctions.Linear;
            for (int i = Audio.Instance.handles.Count - 1; i >= 0; --i)
                Sound.FadeOut(Audio.Instance.handles[i], duration, function);
        }
    }
}

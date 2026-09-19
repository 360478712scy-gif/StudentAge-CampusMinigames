using System;
using UnityEngine;

namespace StudentAge.CampusUno
{
    public static class NativeMinigameResult
    {
        public static void Show(Outcome outcome,GameObject canvas,float resumeScale,Action complete)
        {
            // Native result animations use scaled time. Keep player input locked
            // until the original result callback closes and settles the minigame.
            if(canvas!=null)canvas.SetActive(false);
            // A paused parent menu (NDS) still needs the native result animation to advance.
            // The owning view restores its saved scale when the callback closes it.
            Time.timeScale=resumeScale>0?resumeScale:1f;
            if(outcome==Outcome.Win)HintHelper.ShowWin(complete);
            else if(outcome==Outcome.Draw)HintHelper.ShowEnd(complete);
            else HintHelper.ShowLose(complete);
        }
    }
}

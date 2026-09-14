using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sdk;

namespace StudentAge.CampusUno
{
    public enum ButtonSkin { Green, Orange, White, Go, Subject }
    // Read the installed game's own assets. No native textures or fonts are redistributed.
    public static class NativeSkin
    {
        public static GameObject GuidePrefab;
        public static Sprite Green, Orange, White, Go, Paper, Tips, Desk;
        public static Font TitleFont, BodyFont;
        public static Color TitleColor = new Color(.412f,.169f,.086f);
        public static bool Ready { get; private set; }
        static bool loading; static int pending;
        static readonly List<Action> callbacks = new List<Action>();
        public static void Load(Action complete = null)
        {
            if (Ready && Green!=null && Orange!=null && Paper!=null && TitleFont!=null && GuidePrefab!=null) { if(complete!=null)complete();return; }
            // The game clears resource objects when moving from bootstrap to the menu.
            // A previous completion flag cannot make destroyed Unity objects valid.
            Ready=false;
            if(complete!=null)callbacks.Add(complete);
            if(loading)return;loading=true;pending=2;
            ResMgr.LoadAsync<GameObject>("Prefabs/UI/Guide/GuideImgView@Guide", prefab=>{GuidePrefab=prefab;Finish();});
            ResMgr.LoadAsync<GameObject>("Prefabs/UI/MiniGame/SudokuView@MiniGame", prefab=> {
                if(prefab!=null){
                    foreach(var image in prefab.GetComponentsInChildren<Image>(true)){
                        if(image.sprite==null)continue;
                        switch(image.sprite.name){
                            case "btn_common_green":Green=image.sprite;break;
                            case "btn_common_orange":Orange=image.sprite;break;
                            case "btn_white_2":White=image.sprite;break;
                            case "btn_go_normal":Go=image.sprite;break;
                            case "bg_study":Paper=image.sprite;break;
                            case "bg_exam_2":Desk=image.sprite;break;
                            case "img_yuanjiao_15":Tips=image.sprite;break;
                        }
                    }
                    foreach(var text in prefab.GetComponentsInChildren<Text>(true)){
                        if(text.transform.parent.name=="btn_start")TitleFont=text.font;
                        if(text.name=="txt_operation")BodyFont=text.font;
                    }
                }
                Finish();
            });
        }
        static void Finish()
        {
            if(--pending>0)return;Ready=true;loading=false;
            if(Plugin.Instance!=null)Plugin.Instance.Log("Native UNO skin: green="+(Green!=null)+" orange="+(Orange!=null)+" paper="+(Paper!=null));
            var ready=callbacks.ToArray();callbacks.Clear();foreach(var callback in ready)callback();
        }
        public static Sprite ButtonSprite(ButtonSkin kind){return kind==ButtonSkin.Orange?Orange:kind==ButtonSkin.Go?Go:kind==ButtonSkin.White||kind==ButtonSkin.Subject?White:Green;}
    }
    internal sealed class NativeButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        RectTransform rect;Button button;Vector2 origin;bool hover,down;
        void Start(){rect=(RectTransform)transform;button=GetComponent<Button>();origin=rect.anchoredPosition;}
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;down=false;}
        public void OnPointerDown(PointerEventData e){down=true;}
        public void OnPointerUp(PointerEventData e){down=false;}
        void Update(){if(rect==null)return;bool enabled=button!=null&&button.IsInteractable();float target=enabled&&hover&&!down?1.025f:1;rect.localScale=Vector3.Lerp(rect.localScale,Vector3.one*target,Time.unscaledDeltaTime*20);rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,origin+new Vector2(0,enabled&&down?-3:0),Time.unscaledDeltaTime*25);}
    }
}

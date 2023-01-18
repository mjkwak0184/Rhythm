using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UITapEffect : MonoBehaviour
{
    Transform backgroundTransform;
    SpriteRenderer backgroundSprite;
    Transform[] triangleTransformList = new Transform[6];
    SpriteRenderer[] triangleSpriteList = new SpriteRenderer[6];
    Vector2[] directions = new Vector2[6];
    Quaternion[] rotations = new Quaternion[6];
    Vector2[] targetSizes = new Vector2[6];
    float[] speed = new float[6];
    float runtime = 0;
    bool isActive = false;
    bool canRestart = true;

    static Color[] colors = {
        new Color(0.949f, 1.0f, 0.659f, 0),
        new Color(0.839f, 1.0f, 0.620f, 0),
        new Color(0.725f, 0.961f, 0.957f, 0),
        new Color(0.776f, 0.969f, 0.776f, 0),
        new Color(0.973f, 0.835f, 0.757f, 0),
        new Color(0.945f, 1.0f, 0.655f, 0)
    };
    
    void Start()
    {
        if(!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled){
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }
        Transform scaler = transform.GetChild(0);
        backgroundTransform = scaler.GetChild(0);
        backgroundSprite = backgroundTransform.gameObject.GetComponent<SpriteRenderer>();
        for(int i = 0; i < 6; i++){
            triangleTransformList[i] = scaler.GetChild(i+1);
            triangleSpriteList[i] = triangleTransformList[i].gameObject.GetComponent<SpriteRenderer>();

        }
        reset();
    }

    void reset()
    {
        backgroundTransform.localScale = Vector2.zero;
        backgroundSprite.color = new Color(0.7f, 0.7f, 0.7f, 1.0f);

        for(int i = 0; i < 6; i++){
            triangleTransformList[i].localPosition = Vector3.zero;
            triangleSpriteList[i].color = colors[Random.Range(0, colors.Length)];

            directions[i] = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            speed[i] = Random.Range(0.02f, 0.03f);

            rotations[i] = Random.rotation;

            float targetSize = Random.Range(1.2f, 1.7f);
            targetSizes[i] = new Vector2(targetSize, targetSize);
            triangleTransformList[i].localScale = new Vector2(targetSize - 0.5f, targetSize - 0.5f);

        }
    }


    void Update()
    {
        if(canRestart){  // detect touch and 
            #if UNITY_EDITOR && false
            if(Mouse.current.leftButton.isPressed){
                reset();
                isActive = true;
                canRestart = false;
                runtime = 0;
            }
            #else
            // Register touch input

            foreach(UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches){
                if(touch.phase == UnityEngine.InputSystem.TouchPhase.Began){
                    float width = (float) Screen.width;
                    float height = (float) Screen.height;

                    float xCentered = touch.screenPosition.x - width / 2f;
                    float yCentered = touch.screenPosition.y - height / 2f;

                    float xScaler = 1920f / width;
                    float yScaler = 1080f / height;

                    float aspectRatio = width / height;
                    if(aspectRatio > 16f / 9f){
                        // wider screen, e.g. modern phone
                        xScaler = (1080f * aspectRatio) / width;
                    }else if(aspectRatio < 16f / 9f){
                        // taller screen, e.g. tablet
                        yScaler = (1920f * (1 / aspectRatio)) / height;
                    }

                    float x = xCentered * xScaler;
                    float y = yCentered * yScaler;

                    gameObject.transform.localPosition = new Vector3(x, y, -150);
                    reset();
                    if(!isActive) UIManager.Instance.IncreaseRenderFrame();
                    isActive = true;
                    canRestart = false;
                    runtime = 0;
                }
            }
            #endif
        }

        if(isActive){  // active, continue animation
            for(int i = 0; i < 6; i++){
                triangleTransformList[i].Translate(directions[i] * speed[i]);
                triangleTransformList[i].rotation = Quaternion.LerpUnclamped(triangleTransformList[i].rotation, rotations[i], Time.deltaTime);
                
                triangleTransformList[i].localScale = Vector2.Lerp(triangleTransformList[i].localScale, targetSizes[i], Time.deltaTime);

                Color color = triangleSpriteList[i].color;

                if(runtime < 0.1){
                    color.a += Time.deltaTime * 2;
                }else if(runtime < 0.2){
                    color.a += Time.deltaTime * 15;
                }else if(runtime > 0.3){
                    color.a -= Time.deltaTime * 10;
                }
                triangleSpriteList[i].color = color;

            }
            
            // change opacity & size for background
            Color color2 = backgroundSprite.color;
            backgroundTransform.localScale = Vector2.Lerp(backgroundTransform.localScale, Vector2.one * 1.5f, Time.deltaTime*5);
            if(runtime < 0.05){
                color2.a += Time.deltaTime * 30;
            }else if(runtime < 0.08){
                color2.a += Time.deltaTime * 20;
            }else if(runtime >= 0.08){
                color2.a -= Time.deltaTime * 13;
            }
            backgroundSprite.color = color2;

            if(runtime > 0.5){
                UIManager.Instance.DecreaseRenderFrame();
                isActive = false;
            }
            if(runtime > 0.3){
                canRestart = true;
            }
        }
        // show background

        
        runtime += Time.deltaTime;
    }


    #if ENABLE_LEGACY_INPUT_MANAGER && false
    void Update()
    {
        if(canRestart){  // detect touch and 
            #if UNITY_EDITOR
            if(Input.anyKey){
                reset();
                isActive = true;
                canRestart = false;
                runtime = 0;
            }
            #else
            // Register touch input
            var tapCount = Input.touchCount;
            
            for(var i = 0; i < tapCount; i++){
                var touch = Input.GetTouch(i);
                if(touch.phase == UnityEngine.TouchPhase.Began){
                    float width = (float) Screen.width;
                    float height = (float) Screen.height;
                    
                    float xCentered = touch.position.x - width / 2f;
                    float yCentered = touch.position.y - height / 2f;

                    float xScaler = 1920f / width;
                    float yScaler = 1080f / height;

                    float aspectRatio = width / height;
                    if(aspectRatio > 16f / 9f){
                        // wider screen, e.g. modern phone
                        xScaler = (1080f * aspectRatio) / width;
                    }else if(aspectRatio < 16f / 9f){
                        // taller screen, e.g. tablet
                        yScaler = (1920f * (1 / aspectRatio)) / height;
                    }

                    float x = xCentered * xScaler;
                    float y = yCentered * yScaler;

                    gameObject.transform.localPosition = new Vector3(x, y, -150);
                    reset();
                    if(!isActive) UIManager.Instance.IncreaseRenderFrame();
                    isActive = true;
                    canRestart = false;
                    runtime = 0;
                }
            }
            #endif
        }
        
        if(isActive){  // active, continue animation
            for(int i = 0; i < 6; i++){
                triangleTransformList[i].Translate(directions[i] * speed[i]);
                triangleTransformList[i].rotation = Quaternion.LerpUnclamped(triangleTransformList[i].rotation, rotations[i], Time.deltaTime);
                
                triangleTransformList[i].localScale = Vector2.Lerp(triangleTransformList[i].localScale, targetSizes[i], Time.deltaTime);

                Color color = triangleSpriteList[i].color;

                if(runtime < 0.1){
                    color.a += Time.deltaTime * 2;
                }else if(runtime < 0.2){
                    color.a += Time.deltaTime * 15;
                }else if(runtime > 0.3){
                    color.a -= Time.deltaTime * 10;
                }
                triangleSpriteList[i].color = color;

            }
            
            // change opacity & size for background
            Color color2 = backgroundSprite.color;
            backgroundTransform.localScale = Vector2.Lerp(backgroundTransform.localScale, Vector2.one * 1.5f, Time.deltaTime*5);
            if(runtime < 0.05){
                color2.a += Time.deltaTime * 30;
            }else if(runtime < 0.08){
                color2.a += Time.deltaTime * 20;
            }else if(runtime >= 0.08){
                color2.a -= Time.deltaTime * 13;
            }
            backgroundSprite.color = color2;

            if(runtime > 0.5){
                UIManager.Instance.DecreaseRenderFrame();
                isActive = false;
            }
            if(runtime > 0.3){
                canRestart = true;
            }
        }
        // show background

        
        runtime += Time.deltaTime;
    }
    #endif

    
}
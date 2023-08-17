﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class VoiceImageCanvasSync : MonoBehaviour
{
	/**
	 * Holder of data for a sprite to be updated
	 */

	// new
	public XRDirectInteractor xrInteractor; // Reference to your XR Interactor

	// new
	public void ToggleRayInteraction(bool newState)
	{
		xrInteractor.enabled = newState;
	}

	[System.Serializable]
	public class SpriteSync
	{
		// the Sprite should be set to image reference which sync is using
		public Sprite image;
		// At what time should this sprite be set (from the moment when the parent audio is played, should not be more than the length of the audio clip).
		// Note: setting time for than the length of this audio clip is ignored.
		public float time;
	}

	/**
	 * Holder of data for a canvas to set visibility.
	 */
	[System.Serializable]
	public class CanvasSync
	{
		// which canvas concerned;
		public GameObject canvas;
		// when the action should be done (0 is when the parent audio clip is played) 
		// Note: setting time for than the length of this audio clip is ignored.
		public float time = 0f;
		// the new state of this canvas (Active or not).
		public bool newState = true;
	}

	[System.Serializable]
	public class ObjectData
	{
		public GameObject gameObject;
		public float time = 0.1f;
		public bool newActive = false;
	}

	[System.Serializable]
	public class AnimationStateUpdate
	{
		public AnimationState state;
		public float time = 0.1f;
	}

	[System.Serializable]
	public class ComponentData
	{
		public CustomComponent component;
		public float time = 0.1f;
		public bool newEnabled = false;
	}

	public enum AnimationState { NoUpdate, Idle, Talk,  Ask, Walk, MoveUp, MoveDown, Run, Pick, TurnRight90, TurnRight180,  TurnLeft90, TurnLeft180, Fall, IdleWheelBarrow, PushWheelbarrow, PointRight, PointLeft, Slide, KickBall, Congratulations, SadReaction, Pluck };

	/**
	 * Holder of data for step.
	 */
	[System.Serializable]
	public class VoiceTimingData
	{
		// Audio clip to play.
		public AudioClip voice;

		// sprites that should be used for the image reference.
		public SpriteSync[] sprites;

		// canvasses that should be shown/hidden while this audio is playing.
		public CanvasSync[] canvasses;

		// objects that will be activated/deactivated while this audio is playing.
		public ObjectData[] Objects;

		// objects that will be enabled/disabled while this audio is playing.
		public ComponentData[] Components;

		// Animation states that should be considered while this audio is playing.
		public AnimationStateUpdate[] animationStates;

		// true means audio will stop after playing this clip.
		public bool shouldAudioStop = false;

		// if true, then at this audio ask animation won't be played.
		public bool notAQuestion = false;

		// if this index is >= 0 and this one has a canvas, wrong answer in the interaction with that canvas will set current index to this one.
		public int GoToIndex = -1;

		public bool shouldNotAnimateCharacter = false;

	}


	// index of the current voice playing.
	public int currentAudioIndex = -1;

	// reference to an audio source in the scene.
	public AudioSource audioSource;

	// The image referenced that should be updated by sprites
	public Image imageRef;
	public Image imageRef2;

	/**
	 * Data holder for the current used image component for displaying sprites.
	 */

	[System.Serializable]
	public class ImageRefUpdateData
	{
		// image component
		public Image image;
		// Index at which this component will be used to display sprites.
		public int index = -1;
	}
	// Holder of all image components used during the script.
	public ImageRefUpdateData[] imageRefs;

	// when the audio of this index is reached, if there is a second image ref given then it will be used for displaying sprites and the first will be disabled
	public int imageRefSwitchingIndex = 0;
	// currently used image component.
	private Image UsedImageRef;

	// used to make line renderer stop at canvas
	public GameObject collider;

	// Voice Image Canvas sync data
	public VoiceTimingData[] SyncData;
	// Time at which the last audio clip was played. Used to calculate when to show sprites and cavnasses.
	private float LastAudioClipStartTime = 0f;
	// current used data.
	public VoiceTimingData currentVoiceTimingData;

	// Character body animator
	public Animator animator_body;
	// Character cloth animator
	public Animator animator_cloth;

	/**
	 * Holder of data for what scene should be navigated to after this whole part is done.
	 */
	[System.Serializable]
	public class NextScriptData
	{
		// Scene name.
		public string sceneName;
		// Param with which the scene will be opened with.(ex: class1, class2...)
		public string param;
	}


	public NextScriptData nextScriptData;

	// Parameter used to manage transition between scenes.
	public static string SceneTransitionParam = "";
	// if this is set then when this component is done with data, nextSynchronizer will be enabled.
	public VoiceImageCanvasSync nextSynchronizer;

	// Updates the animator to perform asking animation.
	public void ask()
	{
		updateCharacterAnimationState(AnimationState.Ask);
	}

	// Updates the animator to perform idle animation.
	public void idle()
	{
		updateCharacterAnimationState(AnimationState.Idle);
	}

	// Updates the animator to perform explanation animation.
	public void explain()
	{
		updateCharacterAnimationState(AnimationState.Talk);
	}

	private IEnumerator CallAfterDelay(System.Action Param, float delay)
	{
		yield return new UnityEngine.WaitForSeconds(delay);
		Param();
	}

	public CanvasControllerForClass2 canvasControllerForClass2;

	// Start the next sync.
	public void NextSync()
	{
		print("currentAudioIndex: " + currentAudioIndex);
		if (currentAudioIndex >= SyncData.Length - 1)
		{
			idle();
			if (nextSynchronizer != null)
			{
				nextSynchronizer.enabled = true;
			}
			else if (nextScriptData.sceneName.Length > 1)
			{
				TransitionManager.transitionParam = nextScriptData.param;
				if (SceneManager.GetActiveScene().name != nextScriptData.sceneName)
				{
					TransitionManager.pendingLoadingSceneName = nextScriptData.sceneName;
					SceneManager.LoadScene("LoadingScene");
				}
				else
				{
					SceneManager.LoadScene(nextScriptData.sceneName);
				}
				SceneTransitionParam = nextScriptData.param;


			}
			currentVoiceTimingData = null;
			return;
		}
		currentVoiceTimingData = SyncData[++currentAudioIndex];
		foreach (ImageRefUpdateData data in imageRefs)
		{
			if (data.index == currentAudioIndex && data.image != null)
			{
				UsedImageRef.enabled = false;
				UsedImageRef = data.image;
			}
		}
		if (currentVoiceTimingData.shouldAudioStop)
		{
			if (!currentVoiceTimingData.shouldNotAnimateCharacter && !currentVoiceTimingData.notAQuestion)
			{
				StartCoroutine(CallAfterDelay(() => ask(), currentVoiceTimingData.voice.length - 2f));
			}
			else
			{
				StartCoroutine(CallAfterDelay(() => idle(), currentVoiceTimingData.voice.length - 0.5f));
			}
		}
		audioSource.PlayOneShot(currentVoiceTimingData.voice);
		if (!currentVoiceTimingData.shouldNotAnimateCharacter)
		{
			explain();
		}
		else
		{
			idle();
		}
		LastAudioClipStartTime = Time.time;
		if (canvasControllerForClass2 != null)
		{
			canvasControllerForClass2.PerformCanvasUpdates(currentAudioIndex);
		}
	}

	public void Start()
	{
		StartCoroutine(startSync());
		UsedImageRef = imageRef;
	}

	private IEnumerator startSync()
	{
		yield return new WaitForSeconds(1f);
		NextSync();
	}

	public void updateCharacterAnimationState(AnimationState newState)
	{
		if (animator_body == null) return;
		animator_body.SetBool("idle", false);
		animator_body.SetBool("walk", false);
		animator_body.SetBool("run", false);
		animator_body.SetBool("talk", false);
		animator_body.SetBool("move_up", false);
		animator_body.SetBool("move_down", false);
		animator_body.SetBool("ask", false);
		animator_body.SetBool("turn_right_90", false);
		animator_body.SetBool("turn_right_180", false);
		animator_body.SetBool("pushWheelbarrow", false);
		animator_body.SetBool("idleWheelbarrow", false);
		animator_body.SetBool("congratulations", false);
		animator_body.SetBool("sad_reaction", false);
		animator_body.SetBool("slide", false);
		animator_body.SetBool("pluck", false);
		animator_body.SetBool("kick_ball", false);
		animator_body.SetBool("point_right", false);
		animator_body.SetBool("point_left", false);

		switch (newState)
		{
			case AnimationState.Idle: { animator_body.SetBool("idle", true); break; }
			case AnimationState.Walk: { animator_body.SetBool("walk", true); break; }
			case AnimationState.Run: { animator_body.SetBool("run", true); break; }
			case AnimationState.Pluck: { animator_body.SetBool("pluck", true); break; }
            case AnimationState.Talk: { animator_body.SetBool("talk", true); break; }
            case AnimationState.MoveUp: { animator_body.SetBool("move_up", true); break; }
            case AnimationState.MoveDown: { animator_body.SetBool("move_down", true); break; }
            case AnimationState.PointRight: { animator_body.SetBool("point_right", true); break; }
			case AnimationState.PointLeft: { animator_body.SetBool("point_left", true); break; }
			case AnimationState.Ask: { animator_body.SetBool("ask", true); break; }
			case AnimationState.TurnRight90: { animator_body.SetBool("turn_right_90", true); break; }
			case AnimationState.TurnRight180: { animator_body.SetBool("turn_right_180", true); break; }
			case AnimationState.PushWheelbarrow: { animator_body.SetBool("pushWheelbarrow", true); break; }
			case AnimationState.IdleWheelBarrow: { animator_body.SetBool("idleWheelbarrow", true); break; }
			case AnimationState.Congratulations: { animator_body.SetBool("congratulations", true); break; }
			case AnimationState.SadReaction: { animator_body.SetBool("sad_reaction", true); break; }
			case AnimationState.Slide: { animator_body.SetBool("slide", true); break; }
			case AnimationState.KickBall: { animator_body.SetBool("kick_ball", true); break; }
		}
	}

	private void Update()
	{
		audioSource.pitch = Time.timeScale;
		// check if there is a current voice timing data at first
		if (currentVoiceTimingData == null) return;

		// syncing sprites
		foreach (SpriteSync spriteData in currentVoiceTimingData.sprites)
		{
			float scheduledTime = spriteData.time + LastAudioClipStartTime;
			if (scheduledTime <= Time.time && scheduledTime > Time.time - Time.deltaTime)
			{
				UsedImageRef.transform.parent.gameObject.SetActive(true);
				UsedImageRef.sprite = spriteData.image;
				UsedImageRef.enabled = true;
			}
		}

		// syncing canvasses
		foreach (CanvasSync canvasData in currentVoiceTimingData.canvasses)
		{
			float scheduledTime = canvasData.time + LastAudioClipStartTime;
			if (scheduledTime <= Time.time && scheduledTime > Time.time - Time.deltaTime)
			{
				canvasData.canvas.GetComponent<Canvas>().enabled = canvasData.newState;
				Collider col = canvasData.canvas.GetComponent<BoxCollider>();
				if (col != null) col.enabled = canvasData.newState;
				MeshRenderer meshRenderer = canvasData.canvas.GetComponentInChildren<MeshRenderer>();
				if (meshRenderer != null) meshRenderer.enabled = canvasData.newState;
				MultiStepCanvasInput comp = canvasData.canvas.GetComponent<MultiStepCanvasInput>();
				if (comp != null)
				{
					comp.enabled = canvasData.newState;
				}
				//canvasData.canvas.GetComponent<ControllerSelection.OVRRaycaster>().enabled = canvasData.newState;

				// replacement
				canvasData.canvas.GetComponent<XRDirectInteractor>().enabled = canvasData.newState;


				//collider.SetActive(canvasData.newState);
			}
		}

		// syncing objects
		foreach (ObjectData objData in currentVoiceTimingData.Objects)
		{
			float scheduledTime = objData.time + LastAudioClipStartTime;
			if (scheduledTime <= Time.time && scheduledTime > Time.time - Time.deltaTime)
			{
				objData.gameObject.SetActive(objData.newActive);
				VRObject vrobj = objData.gameObject.GetComponent<VRObject>();
				if (vrobj != null && objData.newActive) vrobj.enabled = true;
			}
		}

		// syncing components
		foreach (ComponentData comp in currentVoiceTimingData.Components)
		{
			float scheduledTime = comp.time + LastAudioClipStartTime;
			if (scheduledTime <= Time.time && scheduledTime > Time.time - Time.deltaTime)
				comp.component.enabled = comp.newEnabled;
		}

		// syncing animations
		foreach (AnimationStateUpdate state in currentVoiceTimingData.animationStates)
		{
			float scheduledTime = state.time + LastAudioClipStartTime;
			if (scheduledTime <= Time.time && scheduledTime > Time.time - Time.deltaTime)
				updateCharacterAnimationState(state.state);
		}

		// if audio clip finished and can play next audio then start next sync
		if (LastAudioClipStartTime != 0f && currentVoiceTimingData.voice.length + LastAudioClipStartTime + 0.2f < Time.time)
			if (!currentVoiceTimingData.shouldAudioStop)
			{
				if (currentVoiceTimingData.GoToIndex >= 0)
					currentAudioIndex = currentVoiceTimingData.GoToIndex;
				NextSync();
			}
			else
			{
				if ((animator_body != null && animator_body.GetBool("ask") == true) || (animator_cloth != null && animator_cloth.GetBool("ask") == true))
					idle();
			}
	}
}

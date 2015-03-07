/// <summary>
/// Joystick.cs v1.0 by Jenny Valdez, jedaniv@gmail.com
/// Adapted from Joystick.js, included in the Unity Standard Assets (Mobile) package.
/// 
/// A generic joystick, adapted to use with uGUI.
/// To use, attach to a gameobject with an inner image and outer image,
/// to visually move the joystick.
/// Then, from any script, ask for joystick.position to get the joystick value.
/// You can also use this to detect any number of taps within the joystick.
/// Visually, you can make the object to fade in and out when it's being clicked,
/// and can fix in in any position or making it free to user input.
/// </summary>

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class Joystick : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
{
	#region Inspector fields

	/// <summary>
	/// Should the GUI be faded when inactive?
	/// </summary>
	public bool fadeGUI = false;

	/// <summary>
	/// Alpha to fade in and out.
	/// </summary>
	public Vector2 fadeAlpha = new Vector2(0f, 0.5f);

	/// <summary>
	/// Whether the joystick is fixed in its place or not.
	/// </summary>
	public bool fixedInSpace = false;

	#endregion

	#region Touch-related fields

	/// <summary>
	/// The touch radius of the joystick.
	/// </summary>
	private float _touchRadius;

	/// <summary>
	/// The last finger identifier.
	/// </summary>
	private int _lastFingerId = -1;

	/// <summary>
	/// How much time there is left for a tap to occur.
	/// </summary>
	private float _tapTimeWindow;

	/// <summary>
	/// The finger down position.
	/// </summary>
	private Vector2 _fingerDownPos;

	/// <summary>
	/// The size of the joystick.
	/// </summary>
	[SerializeField]
	private float _joystickSize;

	/// <summary>
	/// The first delta time.
	/// </summary>
	private float _firstDeltaTime;

	#endregion

	#region GUI fields

	/// <summary>
	/// Inner joystick transform.
	/// </summary>
	private RectTransform _innerGui;

	/// <summary>
	/// Outer joystick transform.
	/// </summary>
	private RectTransform _outerGui;

	/// <summary>
	/// Inner joystick graphic.
	/// </summary>
	private Image _innerGuiImage;

	/// <summary>
	/// Outer joystick graphic.
	/// </summary>
	private Image _outerGuiImage;

	/// <summary>
	/// Plane in which the movement is made.
	/// </summary>
	private RectTransform _movementPlane;

	#endregion

	#region Accessors

	/// <summary>
	/// Current tap count, to detect clicks on the joystick.
	/// </summary>
	public int tapCount { get; protected set; }

	/// <summary>
	/// True if a finger is down.
	/// </summary>
	public bool isFingerDown { get { return _lastFingerId != -1; } }

	/// <summary>
	/// Sets the latched finger.
	/// </summary>
	public int latchedFinger {
		set {
			// If another joystick has latched this finger, we must release it
			if (_lastFingerId == value)
				Restart();
		}
	}

	/// <summary>
	///  The position of the joystick on the screen ([-1, 1] in x,y) for clients to read
	/// </summary>
	public Vector2 position { get; private set; }

	#endregion

	#region Static fields

	/// <summary>
	/// A static collection of all the joysticks of the game.
	/// </summary>
	private static Joystick[] _joysticks;

	/// <summary>
	/// Has the joysticks collection been enumerated yet?
	/// </summary>
	private static bool _enumeratedJoysticks = false;

	/// <summary>
	/// Time allowed between taps.
	/// </summary>
	private static float _tapTimeDelta = 0.25f;

	/// <summary>
	/// Time to tween.
	/// </summary>
	private static float _tweenTime = 0.1f;

	#endregion

	#region Methods

	/// <summary>
	/// Awakes this instance.
	/// </summary>
	private void Awake()
	{
		// Find the joystick components
		try {
			_innerGui = transform.FindChild("Inner").GetComponent<RectTransform>();
			_innerGuiImage = _innerGui.GetComponent<Image>();

			_outerGui = transform.FindChild("Outer").GetComponent<RectTransform>();
			_outerGuiImage = _outerGui.GetComponent<Image>();
		} catch (Exception exp) {
			Debug.LogError("Joystick: " + exp.Message);
			throw;
		}

		// Find the canvas a get the movement plane
		Canvas canvas = UnityExtensions.FindInParents<Canvas>(gameObject);
		_movementPlane = canvas ? canvas.transform as RectTransform : transform as RectTransform;

		// Collect all joysticks in the game, so we can relay finger latching messages
		if (!_enumeratedJoysticks) {
			_joysticks = GameObject.FindObjectsOfType<Joystick>();
			_enumeratedJoysticks = true;
		}

		// Reset the joystick just in case
		if (fadeGUI) {
			_innerGuiImage.CrossFadeAlpha(fadeAlpha.x, 0, true);
			_outerGuiImage.CrossFadeAlpha(fadeAlpha.x, 0, true);
		}
		Restart();
  	}

	/// <summary>
	/// Updates this instance.
	/// </summary>
	private void Update()
	{
		// This is for counting taps over the joystick
		if (_tapTimeWindow > 0) {

			_tapTimeWindow -= Time.deltaTime;

			// When the counter reachs zero, reset the tap count
			if (_tapTimeWindow <= 0)
				tapCount = 0;
		}
	}

	/// <summary>
	/// Sets the position of an object according to some pointer's data.
	/// </summary>
	/// <param name="obj">The object to move.</param>
	/// <param name="data">The pointer's data.</param>
	private void SetPosition(RectTransform obj, PointerEventData data)
	{
		// If the object falls in the rectangle, set its position
		Vector3 globalMousePos;
		if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_movementPlane, data.position, data.pressEventCamera, out globalMousePos))
			obj.position = globalMousePos;
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Restarts this instance.
	/// </summary>
	public void Restart()
	{
		// Release the finger control and set the joystick back to the default position
		_lastFingerId = -1;
		position = Vector2.zero;
		_fingerDownPos = Vector2.zero;

		// If the GUI is set to fade, do it
		if (fadeGUI)
			Toggle(false);
		// Reset the GUI's position only if fading deactivated (looks ugly otherwise)
		else
			_innerGui.position = _outerGui.position;
	}

	/// <summary>
	/// Fades in and out the joystick.
	/// </summary>
	/// <param name="on"><c>true</c> if fading in, fade out otherwise.</param>
	public void Toggle(bool on)
	{
		_innerGuiImage.CrossFadeAlpha(on ? fadeAlpha.y : fadeAlpha.x, _tweenTime, true);
		_outerGuiImage.CrossFadeAlpha(on ? fadeAlpha.y : fadeAlpha.x, _tweenTime, true);
	}

	/// <summary>
	/// Raises the pointer down event.
	/// </summary>
	/// <param name="data">Data.</param>
	public void OnPointerDown(PointerEventData data)
	{
		// If we are already being touched, do nothing
		if (_lastFingerId != -1)
			return;

		// Latch the finger if this is a new touch
		_lastFingerId = data.pointerId;

		// Set the center of the joystick in the finger if not fixed in space
		// Otherwise, set it on the outer GUI's position
		_fingerDownPos = !fixedInSpace ? data.position : RectTransformUtility.WorldToScreenPoint(data.pressEventCamera, _outerGui.position);

		// Calculate limits for the joystick
		float xMin = Mathf.Clamp(_fingerDownPos.x - _joystickSize * 0.5f, 0, Screen.width);
		float xMax = Mathf.Clamp(_fingerDownPos.x + _joystickSize * 0.5f, 0, Screen.width);
		float yMin = Mathf.Clamp(_fingerDownPos.y - _joystickSize * 0.5f, 0, Screen.height);
		float yMax = Mathf.Clamp(_fingerDownPos.y + _joystickSize * 0.5f, 0, Screen.height);

		_touchRadius = Mathf.Max(xMax - xMin, yMax - yMin) * 0.5f;

		// Accumulate taps if it is within the time window
		tapCount = _tapTimeWindow > 0 ? tapCount + 1 : 1;
		_tapTimeWindow = _tapTimeDelta;

		// Tell other joysticks we've latched this finger
		foreach (Joystick j in _joysticks)
			if (j != this)
				j.latchedFinger = _lastFingerId;

		// Set the GUI's initial position where the finger is
		OnDrag(data);

		// Only center the outer joystick position if the joystick isn't fixed in the space
		if (!fixedInSpace)
			_outerGui.position = _innerGui.position;

		// Set the GUI's color if the fading is activated
		if (fadeGUI)
			Toggle(true);
	}

	/// <summary>
	/// Raises the pointer up event.
	/// </summary>
	/// <param name="data">Data.</param>
	public void OnPointerUp(PointerEventData data)
	{
		// Release only if the lifted finger is the one latched
		if (_lastFingerId == data.pointerId)
			Restart();
	}

	/// <summary>
	/// Raises the drag event.
	/// </summary>
	/// <param name="data">Data.</param>
	public void OnDrag(PointerEventData data)
	{
		// If the finger dragged isn't the latched one, return
		if (data.pointerId != _lastFingerId)
			return;

		Vector2 touchPosition = Vector2.ClampMagnitude(data.position - _fingerDownPos, _touchRadius);

		position = new Vector2(
			Mathf.Clamp(touchPosition.x / _touchRadius, -1, 1), 
			Mathf.Clamp(touchPosition.y / _touchRadius, -1, 1));

		// Set the GUI's position
		data.position = touchPosition + _fingerDownPos;
		SetPosition(_innerGui, data);
	}

	#endregion
}
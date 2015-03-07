/// <summary>
/// Joystick.cs v1.0 by Jenny Valdez, jedaniv@gmail.com
/// 
/// Implementation of the Singleton pattern for monobehaviours.
/// It's missing a thread lock inside the getter.
/// </summary>

using UnityEngine;
using System;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    /// <summary>
    /// The instance.
    /// </summary>
    private static T _instance = null;

    /// <summary>
    /// Lazy instantiation of this behaviour.
    /// </summary>
    /// <value>The instance.</value>
	public static T instance {
		get {
            // If there are no instances
			if (_instance == null) {
                // Try to find any instance in the game.
				_instance = GameObject.FindObjectOfType<T>();

                // If there are no instances, create one.
				if (_instance == null) {
					Type type = typeof(T);
					string typename = type.ToString();

					Debug.LogWarning("No instance of " + typename + ", creating a temporary one.");
					_instance = new GameObject("Temp_" + typename, type).GetComponent<T>();

                    // If the instance is still null, there was a fatal error.
					if (_instance == null) {
						Debug.LogError("Problem during the creation of " + typename);
						return null;
					}
				}

                // Initialize it
				_instance.Init();
			}

			return _instance;
		}
	}

    /// <summary>
    /// Awakes this instance.
    /// </summary>
	private void Awake()
	{
		if (_instance == null) {
			_instance = this as T;
			_instance.Init();
		}
	}

    /// <summary>
    /// Inits this instance.
    /// </summary>
    /// <remarks>
    /// Please use this instead of Awake.
    /// </remarks>
	protected virtual void Init() { }

    /// <summary>
    /// Raises the application quit event.
    /// </summary>
	private void OnApplicationQuit()
	{
		_instance = null;
	}

    /// <summary>
    /// Checks whether an instance exists or not.
    /// </summary>
    /// <returns><c>true</c>, if this was instanced, <c>false</c> otherwise.</returns>
	public static bool InstanceExists()
	{
		return _instance != null;
	}
}

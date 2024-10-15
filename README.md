# Scene Reference Attribute

[![openupm](https://img.shields.io/npm/v/com.kylewbanks.scenerefattribute?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.kylewbanks.scenerefattribute/)

Unity C# attribute for serializing **component and interface references** within the scene or prefab during `OnValidate`, rather than using `GetComponent*` functions in `Awake/Start/OnEnable` at runtime. 

This project aims to provide:

- a simple set of tags for declaring dependency locations
- resolution, validation and serialisation of references **(including interface types!)**
- determinate results so you never have to worry about Unity losing your references, they'll always be safely regenerated

### Installation

**Basic**

You can simply clone (or download this repo as a zip and unzip) into your `Assets/` directory. 

**UPM**

[Install with UPM](https://openupm.com/packages/com.kylewbanks.scenerefattribute) on the command-line like so:

```
openupm add com.kylewbanks.scenerefattribute
```

### Why?

Mainly to avoid framerate hiccups when additively loading scenes in [Farewell North](https://store.steampowered.com/app/1432850/Farewell_North/), but I also find this to be a much cleaner way to declare references.

For more details on how this project came about, check out this [Farewell North devlog on YouTube](https://youtu.be/lpBIbmTPDQc).

### How?

Instead of declaring your references, finding them in `Awake` or assigning them in the editor, and then validating them in `OnValidate` like so: 

```cs
// BEFORE

[SerializeField] private Player _player; 
[SerializeField] private Collider _collider;
[SerializeField] private Renderer _renderer;
[SerializeField] private Rigidbody _rigidbody;
[SerializeField] private ParticleSystem[] _particles;
[SerializeField] private Button _button;

private void Awake()
{
    this._player = Object.FindObjectByType<Player>();
    this._collider = this.GetComponent<Collider>();
    this._renderer = this.GetComponentInChildren<Renderer>();
    this._rigidbody = this.GetComponentInParent<Rigidbody>();
    this._particles = this.GetComponentsInChildren<ParticleSystem>();
}

private void OnValidate()
{
    Debug.Assert(this._button != null, "Button must be assigned in the editor");
}
```

You can declare their location inline using the `Self`, `Child`, `Parent`, `Scene` and `Anywhere` attributes:

```cs
// AFTER

[SerializeField, Scene] private Player _player; 
[SerializeField, Self] private Collider _collider;
[SerializeField, Child] private Renderer _renderer;
[SerializeField, Parent] private Rigidbody _rigidbody;
[SerializeField, Child] private ParticleSystem[] _particles;
[SerializeField, Anywhere] private Button _button;

private void OnValidate()
{
    this.ValidateRefs();
}
```

The `ValidateRefs` function is made available as a `MonoBehaviour` extension for ease of use on any `MonoBehaviour` subclass, and handles finding, validating and serialisating the references in the editor so they're always available at runtime. Alternatively you can extend `ValidatedMonoBehaviour` instead and `ValidateRefs` will be invoked automatically. 

### Serialising Interfaces 

By default Unity doesn't allow you to serialise interfaces, but this project allows it by wrapping them with the `InterfaceRef` type, like so:

```cs
// Single interface
[SerializeField, Self] private InterfaceRef<IDoorOpener> _doorOpener; 

// Array of interfaces
[SerializeField, Self] private InterfaceRef<IDoorOpener>[] _doorOpeners; 
```

From here they'll be serialised like any other component reference, with the interface available using `.Value`: 

```cs
IDoorOpener doorOpener = this._doorOpener.Value;
doorOpener.OpenDoor();
```

### Attributes

- `Self` looks for the reference on the same game object as the attributed component using `GetComponent(s)()`
- `Parent` looks for the reference on the parent hierarchy of the attributed components game object using `GetComponent(s)InParent()`
- `Child` looks for the reference on the child hierarchy of the attributed components game object using `GetComponent(s)InChildren()`
- `Scene` looks for the reference anywhere in the scene using `FindAnyObjectByType` and `FindObjectsOfType`
- `Anywhere` will only validate the reference isn't null, but relies on you to assign the reference yourself.

### Flags

The attributes all allow for optional flags to customise their behaviour: 

```cs
[SerializeField, Self(Flag.Optional)]    
private Collider _collider;

[SerializeField, Parent(Flag.IncludeInactive)]
private Rigidbody _rigidbody;
```

You can also specify multiple flags: 

```cs
[SerializeField, Child(Flag.Optional | Flag.IncludeInactive)] 
private ParticleSystem[] _particles;
```

- `Optional` won't fail validation for empty (or null in the case of non-array types) results.
- `IncludeInactive` only affects `Parent`, `Child` and `Scene`, and determines whether inactive components are included in the results. By default, only active components will be found, same as `GetComponent(s)InChildren`, `GetComponent(s)InParent` and `FindObjectsOfType`. 
- `ExcludeSelf` only affects `Parent` and `Child`, and determines whether components on this `GameObject` are included in the results. By default, components on this `GameObject` are included.
- `Editable` only affects `Parent`, `Child` and `Scene`, and allows you to edit the resulting reference. This is useful, for example, if you have two children who would satisfy a reference but you want to manually select which reference is used. 
- `Filter` allows you to implement custom logic to filter references. See **Filters** below.
- `EditableAnywhere` has the same effect as `Editable` but will not validate the supplied reference. This allows you to manually specify a reference with an automatic fallback.
### Filters

This project also provides support for filtering references based on custom logic. 

Let's use an example to demonstrate: imagine you have a `StealthAnimations` class which applies stealth-related game logic to all child `Animators`.

```cs
public class StealthAnimations : MonoBehaviour
{
    private static readonly int ANIMATOR_PARAM_STEALTH_STATE = Animator.StringToHash("StealthState");

    [SerializeField, Child] private Animator[] _animators;
    
    private void Update()
    {
        int state = GetStealthState();
        for (int i = 0; i < this._animators.Length; i++)
            this._animators[i].SetInteger(ANIMATOR_PARAM_STEALTH_STATE, state);
    }
}
```

This will reference all child animators and apply the `StealthState` integer each frame. The issue here is that not all child animators have a `StealthState` parameter. One way to solve this would be to check each animator to see if it has the parameter, but this would be inefficient, especially if we have many child animators but only a few have the parameter. 

Instead, let's pre-filter the animators by implementing a `SceneRefFilter` and setting the `filter` parameter on the attribute: 

```cs
public class StealthAnimations : MonoBehaviour
{
    
    private class AnimatorRefFilter : SceneRefFilter<Animator>
    {
        public override bool IncludeSceneRef(Animator animator)
            => AnimatorUtils.HasParameter(animator, ANIMATOR_PARAM_STEALTH_STATE);
    }
    
    [SerializeField, Child(filter: typeof(AnimatorRefFilter))] 
    private Animator[] _animators;
}
```

Our custom `AnimatorRefFilter` will be invoked for each `Animator` matching our ref attribute (`Child`) and flags, but only the ones where `true` is returned will be included.   

You can apply this pattern to any type and any condition, allowing you to pre-filter references to ensure they are what you'll actually need at runtime. 

Additional examples:
- Filter to include/exclude trigger colliders
- Filter to include/exclude if another component is present
- Filter to include/exclude static gameObjects
- etc.

**Note:** this filter is only invoked at edit time, so you can't rely on any runtime information for filtering. In that case you will still need to filter your references in Awake or similar.  

### Features 

- Supports all `MonoBehaviour` and `Component` types (basically anything you can use with `GetComponent`), plus interfaces!
- Determinate results, so there's no worry of Unity forgetting all your serialised references (which has happened to me a few times on upgrading editor versions). All references will be reassigned automatically.  
- Fast, this tool won't slow down your editor or generate excessive garbage. 
- Fully serialised at edit time, so all references are already available at runtime
- The `ValidateRefs()` function is made available as a `MonoBehaviour` extension for ease of use. You can also invoke it outside of a `MonoBehaviour` like so, if preferred: 
```cs
MyScript script = ...;
SceneRefAttributeValidator.Validate(script);
```
- One-click to regenerate all references for every script under `Tools > Validate All Refs`.
- Regenerate references for any component by right-clicking the component and selecting `Validate Refs`.
![image](https://user-images.githubusercontent.com/2164691/215190393-192083fc-4c83-42da-8ca4-a93d2349aaa2.png)

### License

This project is made available under the [MIT License](./LICENSE), so you're free to use it for any purpose.

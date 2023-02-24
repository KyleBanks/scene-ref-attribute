# Scene Reference Attribute

Unity C# attribute for serializing **component and interface references** within the scene or prefab during `OnValidate`, rather than using `GetComponent*` functions in `Awake/Start/OnEnable` at runtime. 

This project aims to provide:

- a simple set of tags for declaring dependency locations
- resolution, validation and serialisation of references **(including interface types!)**
- determinate results so you never have to worry about Unity losing your references, they'll always be safely regenerated

### Why?

Mainly to avoid framerate hiccups when additively loading scenes in [Farewell North](https://store.steampowered.com/app/1432850/Farewell_North/), but I also find this to be a much cleaner way to declare references.

For more details on how this project came about, check out this [Farewell North devlog on YouTube](https://youtu.be/lpBIbmTPDQc).

### How?

Instead of declaring your references, finding them in `Awake` or assigning them in the editor, and then validating them in `OnValidate` like so: 

```cs
[SerializeField] private Player _player; 
[SerializeField] private Collider _collider;
[SerializeField] private Renderer _renderer;
[SerializeField] private Rigidbody _rigidbody;
[SerializeField] private ParticleSystem[] _particles;

private void Awake()
{
    this._collider = this.GetComponent<Collider>();
    this._renderer = this.GetComponentInChildren<Renderer>();
    this._rigidbody = this.GetComponentInParent<Rigidbody>();
    this._particles = this.GetComponentsInChildren<ParticleSystem>();
}

private void OnValidate()
{
    Debug.Assert(this._player != null, "Player must be assigned in the editor");
}
```

You can declare their location inline using the `Self`, `Child`, `Parent` and `Anywhere` attributes:

```cs
[SerializeField, Anywhere] private Player _player; 
[SerializeField, Self] private Collider _collider;
[SerializeField, Child] private Renderer _renderer;
[SerializeField, Parent] private Rigidbody _rigidbody;
[SerializeField, Child]  private ParticleSystem[] _particles;

private void OnValidate()
{
    this.ValidateRefs();
}
```

The `ValidateRefs` function is made available as a `MonoBehaviour` extension for ease of use, and handles finding, validating and serialisating the references in the editor so they're always available at runtime. 

### Serialising Interfaces

By default Unity doesn't allow you to serialise interfaces, but this project allows it by wrapping them with the `InterfaceRef` type, like so:

```cs
[SerializeField, Self] private InterfaceRef<IDoorOpener> _doorOpener;
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
- `IncludeInactive` only affects `Parent` and `Child`, and determines whether inactive components are included in the results. By default, only active components will be found, same as `GetComponent(s)InChildren` and `GetComponent(s)InParent`. 


### Features 

- Supports all `MonoBehaviour` and `Component` types (basically anything you can use with `GetComponent`) 
- Determinate results, so there's no worry of Unity losing all your serialised references (which has happened to me a few times on upgrading editor versions). All references will be reassigned automatically.  
- Fast, this tool won't slow down your editor or generate excessive garbage. 
- Fully serialised at edit time, so all references are already available at runtime
- The `ValidateRefs` function is made available as a `MonoBehaviour` extension for ease of use. You can also invoke it outside of a `MonoBehaviour` like so, if preferred: 
```cs
MyScript script = ...;
SceneRefAttributeValidator.Validate(script);
```
- One-click to regenerate all references for every script under `Tools > Validate All Refs`.
- Regenerate references for any component by right-clicking the component and selecting `Validate Refs`.
![image](https://user-images.githubusercontent.com/2164691/215190393-192083fc-4c83-42da-8ca4-a93d2349aaa2.png)

### Limitations

- Supports arrays but not `List`s. Feel free to submit a PR if you'd like to see this:
    - **Valid**: `[Child] Renderer[] _renderers;`
    - **Invalid**: `[Child] List<Renderer> _renderers;`
- Support arrays of InterfaceRef<IInterface>.
    - **Valid**: `[Child] InterfaceRef<IInterface>[] _interfaces;`
    - **Invalid**: `[Child] InterfaceRef<IInterface[]> _interfaces;`

### License

This project is made available under the [MIT License](./LICENSE), so you're free to use it for any purpose.

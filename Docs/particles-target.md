# Empty Platformer project - Documentation - `Particles Target`

Modify particles emission shape to move them smoothly to a given target.

![`Particles Target` component inspector](./images/particles-target.png)

![`Particles Target` component preview](./images/particles-target-example.gif)

## Parameters

- **Particle System**: The reference to the `Particle System` component affected by this effect.
- **Target**: The position to which the particles will move.
- **Lerp**: The linear interpolation amount from the current particle position to the target the particles will move along each frame.
- **Affect Particles Delay**: Defines the particles before this component affect the particles position. Before this delay, the particles will ask as they normally do from the Particle System.
- **Force World Space**: If `true`, sets the referenced Particle System's *Simulation Space*  to *World*. Note that in *Local* simulation spaces, the position of the target will be relative to the particle emitter position. So, to avoid any confusion, enabling this option ensures you that the given ***Target*** is the actual target position of the particles.

## Public API

### Accessors

#### `Particles`

```cs
public ParticleSystem Particles { get; set; }
```

Reference to the "Particle System" component affected by this effect.

#### `Target`

```cs
public Transform Target { get; set; }
```

The position to which the particles will move.

#### `Lerp`

```cs
public float Lerp { get; set; }
```

The linear interpolation amount from the current particle position to the target the particles will move along each frame.

#### `Lerp`

```cs
public float AffectParticlesDelay { get; set; }
```

Defines the particles before this component affect the particles position. Before this delay, the particles will ask as they normally do from the Particle System.

---

[<= Back to summary](./README.md)
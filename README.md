
![GraphNodeRelax](https://github.com/Ohmnivore/GraphNodeRelax/assets/3769354/ccc4ae22-7d1f-473b-945a-37bc44518e8e)

# Graph Node Relax
This is a Unity port of the [Node Relax Blender Addon](https://www.youtube.com/watch?v=QvNz3ON6e1I). It allows to organize and align nodes with a brush, by painting over them.

# Compatibility
This plugin is compatible with systems built with Unity's [GraphView API](https://docs.unity3d.com/ScriptReference/Experimental.GraphView.GraphView.html).

Tested compatible systems are:

* [Shader Graph](https://docs.unity3d.com/Manual/com.unity.shadergraph.html)
* [VFX Graph](https://unity.com/visual-effect-graph)

It's possible to adapt it to other GraphView-based systems, should they not be compatible out of the box. (see `IGraphViewBuilder` and `DefaultGraphViewBuilder`)

# Default Keyboard Shortcuts
* Toggle brush: Shift + R
* Disable brush: Escape
* Change brush radius: - and =/+

# Settings
Customize the shorcuts and more under: Edit > Preferences > Graph Node Relax

# Acknowledgements
* [Node Relax Blender Addon](https://github.com/specoolar/NodeRelax-Blender-Addon) by [Shahzod Boyhonov](https://twitter.com/specoolar)
* [1€ Filter implementation](https://github.com/DarioMazzanti/OneEuroFilterUnity) by [Dario Mazzanti](https://www.dariomazzanti.com)

# Graph Tools Foundation
GraphView-specific code has been abstracted away in anticipation. Still, some amount of new code would need to be written for a GTF-specific brush manipulator, relaxer, and cache builder.

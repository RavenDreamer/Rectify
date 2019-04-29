![Rectify](https://github.com/RavenDreamer/Rectify/blob/master/GitHub/logo.png "Rectify Logo")

# An RSR pathfinding implementation

### Overview ###

Rectify is a pathfinding library designed for static or semi-static square tilegrids in 2d. It decomposes the problem space into a minimum number of linked rectangles, then uses that rectangular navmesh to calculate an optimal path using a techinque known as Rectangular Symmetry Reduction (RSR). The goal with Rectify was to remove the hassle associated with implementing pathfinding -- pass in your terrain data, provide a start and end position, and receive the shortest path between the two!

[How it Works -- Part 1: Rectangular Decomposition](https://github.com/RavenDreamer/Rectify/wiki/How-it-Works----Part-1:-Rectangular-Decomposition)

[How it Works -- Part 2: Rectangular Symmetry Reduction](https://github.com/RavenDreamer/Rectify/wiki/How-it-Works----Part-2:-Rectangular-Symmetry-Reduction)

### Should I use Rectify in my game or project? ###

The Rectangular decomposition technique used by Rectify limits it to 2d grid-aligned maps only, such as Dwarf Fortress, Terraria, and the like. Rectify is not suitable if you have walls at anything other than 90°. Concave regions and regions with holes are handled by the alogirthm without issue. Rectify has been tested with 275x275 tile maps, but as with most algorithms, the largest maps may run into performance issues regardless.

Due to the fact that it bases its pathfinding upon the navmesh it generates, Rectify *does* have a persistent memory footprint, unlike some A* algorithms which do not require anything when they are not actively searching.
Rectify only provides agent-independent paths. If you need to avoid intersecting with other agents, for instance, you will need to implement that separately.

### Getting Started ###
This project has two public-facing classes, "Rectify" (Used for the decomposition) and "RectifyPathfinder" (Used for pathfinding).

See the [Getting Started](https://github.com/RavenDreamer/Rectify/wiki/Getting-Started) page for more information.

### Credits ###
RectifyPathfinder makes use of BlueRaja's [High Speed Priority Queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp) as part of its RSR implementation.

### Acknowledgements ###
Rectify was developed in response to playing Dwarf Fortress, which is subject to so-called "FPS death", where the game gradually slows down due to the volume of calculations it must perform each frame. Many fans suspect pathfinding is one of the major culprits here, and it is commonly suggested to block off unused areas of the map in order to simplify those calculations.

The idea for Rectify came to me after looking at my Dwarven Fortresses and noticing that most of the pathing was between specific points of interest - Farms to Kitchen, Quarry to Storehouse, etc. - and that the paths between those locations were filled with straight lines. This lead me to independently conceive of the concept of a "NavMesh" (once I realized they were called this, it was much easier to research; it's quite hard to look up a particular concept if you lack the requisite domain knowledge of what that concept is properly known as!).

As such, I don't believe this library to be novel in any sense other than implementation, perhaps. Hopefully it helps you, and best of luck with your own projects!

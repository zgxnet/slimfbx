# AI Agents for SlimFbx

**Project Mission:** Provide an open, efficient "Slim FBX" format. We aim to represent the standard FBX structure using the **CBOR** binary format, offering a robust .NET API to read and write these files.

**Notes**
- **`test2026/`**: Local and should be ommited.

This document defines the specialized AI agent personas and their responsibilities for the SlimFbx project. These definitions help contextualize tasks for AI assistants working on this repository.

## 1. Core Architect Agent
**Role:** Maintain the structural integrity of the library.
**Focus:** 
- **Architecture:** Managing the public API surface and internal component decoupling.
- **Environment:** Ensuring compatibility with **.NET 10.0** and maximizing the usage of modern C# features.
- **Dependencies:** Managing integrations with `Stride.CommunityToolkit` and `PeterO.Cbor`.
- **Memory Management:** Optimizing the use of `unsafe` blocks and `MemoryWriter` for high-performance I/O.

## 2. FBX Domain Specialist
**Role:** Expert in 3D formats, Animation, and Geometry.
**Context:**
- **Scene Graph:** Handling `Node`, `Scene`, and `FbxObject` hierarchies.
- **Math:** Implementing complex 3D math (TRS transforms, Euler rotations, Bounding Spheres).
- **Animation:** Managing `AnimStack`, `AnimLayer`, `AnimCurve`, and `AnimCurveKey`.
- **Deformation:** Handling `Skin`, `Cluster`, and `Deformer` logic for mesh skinning.
- **Geometry:** Processing `Mesh`, `Geometry`, and `LayerElement` data structures.
 (Read/Write).
**Context:**
- **CBOR:** Maintaining `CborLoader`, `CborConverter`, and `CborUtil` for efficient binary serialization/de
**Context:**
- **CBOR:** Maintaining `CborLoader`, `CborConverter`, and `CborUtil` for efficient binary serialization using `PeterO.Cbor`.
- **Interoperability:** Managing the `ExternalFbxSdk` interactions and local `lib/fbx2slim.dll` usage.
- **File Ops:** Robust file handling in `FileOps.cs`.


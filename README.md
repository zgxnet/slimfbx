# SlimFbx

SlimFbx is a high-performance .NET library designed to read and write "Slim FBX" files—a binary representation of the standard FBX structure using **CBOR** (Concise Binary Object Representation). 

The goal of this project is to provide an open, efficient, and robust alternative for handling 3D asset data in .NET applications, with a focus on modern C# features and minimal overhead.

## Key Features

- **Efficient Binary Format:** Uses CBOR to represent FBX data, ensuring compact file sizes and fast loading times.
- **Modern .NET Stack:** Built for **.NET 10.0** (and compatible with .NET 8.0), leveraging the latest performance improvements in the ecosystem.
- **Comprehensive 3D Support:**
  - **Scene Graph:** Full hierarchy support (`Node`, `Scene`, `FbxObject`).
  - **Geometry:** Handles `Mesh`, `Geometry`, `Skin`, and `Deformer` data.
  - **Animation:** robust support for `AnimStack`, `AnimLayer`, and animation curves.
  - **Math:** Built-in structures for TRS transforms, Euler rotations, and bounding volumes.
- **Interop Capable:** Includes utilities for working with the official FBX SDK via `ExternalFbxSdk`.

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or .NET 8.0 for compatibility modes)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/SlimFbx.git
   ```
2. Navigate to the project directory:
   ```bash
   cd SlimFbx
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```

## Usage

*Coming soon: Detailed examples on loading a scene and exporting meshes.*

## Project Structure

- **`SlimFbx/`**: Core library containing the CBOR converters, scene graph objects, math utilities, and IO logic.

## Contributing

Contributions are welcome! Please see [AGENTS.md](AGENTS.md) for an overview of the architectural roles and domain specializations within the project.

## License

*License information to be added.*

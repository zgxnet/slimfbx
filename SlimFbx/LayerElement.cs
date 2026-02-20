namespace SlimFbx;

public class LayerElement
{
    /**	\enum EMappingMode     Determines how the element is mapped to a surface.
    * - \e eNone                  The mapping is undetermined.
    * - \e eByControlPoint      There will be one mapping coordinate for each surface control point/vertex.
    * - \e eByPolygonVertex     There will be one mapping coordinate for each vertex, for every polygon of which it is a part.
                                  This means that a vertex will have as many mapping coordinates as polygons of which it is a part.
    * - \e eByPolygon            There can be only one mapping coordinate for the whole polygon.
    * - \e eByEdge               There will be one mapping coordinate for each unique edge in the mesh.
                                  This is meant to be used with smoothing layer elements.
    * - \e eAllSame              There can be only one mapping coordinate for the whole surface.
    */
    public enum EMappingMode
    {
        None,
        ByControlPoint,
        ByPolygonVertex,
        ByPolygon,
        ByEdge,
        AllSame
    }

    /** \enum EReferenceMode     Determines how the mapping information is stored in the array of coordinates.
  * - \e eDirect              This indicates that the mapping information for the n'th element is found in the n'th place of 
                              FbxLayerElementTemplate::mDirectArray.
  * - \e eIndex,              This symbol is kept for backward compatibility with FBX v5.0 files. In FBX v6.0 and higher, 
                              this symbol is replaced with eIndexToDirect.
  * - \e eIndexToDirect     This indicates that the FbxLayerElementTemplate::mIndexArray
                              contains, for the n'th element, an index in the FbxLayerElementTemplate::mDirectArray
                              array of mapping elements. eIndexToDirect is usually useful for storing eByPolygonVertex mapping 
                              mode elements coordinates. Since the same coordinates are usually
                              repeated many times, this saves spaces by storing the coordinate only one time
                              and then referring to them with an index. Materials and Textures are also referenced with this
                              mode and the actual Material/Texture can be accessed via the FbxLayerElementTemplate::mDirectArray
  */
    public enum EReferenceMode
    {
        Direct,
        Index,
        IndexToDirect
    };

    public EMappingMode MappingMode;

    public EReferenceMode ReferenceMode;
}

public class LayerElement<T> : LayerElement
{
    public T[] Data = [];
}

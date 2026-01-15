namespace SKSSL.Textures;

public abstract partial class TextureLoader
{
    /// <summary>
    /// Internal Material Registry for Texture Loader class. Utilized for any kind of object that requires more than one map.
    /// Handles multiple map-types.
    /// </summary>
    public static class MaterialRegistry
    {
        /// The maximum number of materials the game is willing to load at any given runtime instance.
        private const int MaxMaterials = 2048;

        /// Used as numerical ID selector for new materials, as well as total material counter. 
        public static int MaterialCount { get; private set; } = 0;

        /// Materials used by the game. <seealso cref="SKMaterial"/>
        public static readonly SKMaterial[] Materials = new SKMaterial[MaxMaterials];

        /// Only used during loading. Assigns a material name to a <see cref="SKMaterial"/>'s integer ID.
        public static readonly Dictionary<string, int> NameToId = new(MaxMaterials);

        /// <summary>
        /// Registers or gets an existing material ID by name.
        /// Called during loading when a multi-texture folder is processed.
        /// </summary>
        public static int RegisterMaterial(string name, SKMaterial material)
        {
            if (NameToId.TryGetValue(name, out int existingId))
                return existingId;

            if (MaterialCount >= MaxMaterials)
                throw new InvalidOperationException($"Exceeded maximum material count ({MaxMaterials})");

            int newId = MaterialCount++;
            Materials[newId] = material;
            NameToId[name] = newId;

            return newId;
        }

        /// <summary>
        /// Fast access by ID — used heavily at runtime.
        /// <remarks>
        /// If id &lt; 0, or id &gt; Material Count, use Default Error Material.
        /// Otherwise, utilize Materials[id] entry.
        /// </remarks>
        /// </summary>
        public static SKMaterial GetMaterial(int id)
            => id < 0 || id >= MaterialCount ? DefaultErrorMaterial : Materials[id];

        /// <summary>
        /// Lookup by name (only for debugging/tools)
        /// </summary>
        public static int GetId(string name) => NameToId.GetValueOrDefault(name, -1);

        /// <summary>
        /// Overload for <see cref="GetMaterial(int)"/> that attempts to try-get value.
        /// </summary>
        /// <param name="reference"><see cref="string"/> reference id name for material.</param>
        /// <returns>Material by reference id, or <see cref="DefaultErrorMaterial"/></returns>
        /// <remarks>Typically reference is "folder_..._folder_texture"</remarks>
        /// <example>GetMaterial("gneiss_rock");</example>
        public static SKMaterial GetMaterial(string reference)
            => NameToId.TryGetValue(reference, out int id) ? Materials[id] : DefaultErrorMaterial;

        /// <summary>
        /// Default material with error and null texture mappings.
        /// </summary>
        private static readonly SKMaterial DefaultErrorMaterial = new()
        {
            Diffuse = HardcodedTextures.GetErrorTexture(),
            Normal = HardcodedTextures.GetErrorTexture(),
            // Emissive, Displacement, and the rest can stay null.
        };
    }
}
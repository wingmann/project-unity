using UnityEngine;

namespace Wingmann.Project.Entities
{
    /// <summary>
    /// Implements entity identifier.
    /// </summary>
    public class Identifier : MonoBehaviour
    {
        /// <summary>
        /// Classification type.
        /// </summary>
        public EntityType EntityType { get; }

        /// <summary>
        /// String representation of concrete type of entity.
        /// </summary>
        public string NameOfEntytyType { get; set; } = string.Empty;

        /// <summary>
        /// Initialize unnamed entity.
        /// </summary>
        /// <param name="entityType">Entyty type.</param>
        public Identifier(EntityType entityType)
        {
            EntityType = entityType;
        }

        /// <summary>
        /// Initiazie full 
        /// </summary>
        /// <param name="entityType">Entyty type.</param>
        /// <param name="nameOfType">Name of entity type.</param>
        public Identifier(EntityType entityType, string nameOfType)
        {
            EntityType = entityType;
            NameOfEntytyType = nameOfType;
        }

        // Returns internal name of entity.
        private string SpecifyIdentifierAsPrefix()
        {
            return "1000";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => SpecifyIdentifierAsPrefix();
    }
}

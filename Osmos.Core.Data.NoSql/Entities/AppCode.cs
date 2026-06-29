using System.ComponentModel.DataAnnotations;

namespace Osmos.Core.Data.NoSql.Entities
{
    public class AppCode : Entity
    {
        [Required]
        public string AppId { get; set; }
        [Required]
        public string[] Keys { get; set; }

        public override bool Equals(object obj)
        {
            var code = (AppCode)obj;

            if (AppId != code.AppId) return false;

            if (Keys.Length != code.Keys.Length) return false;

            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != code.Keys[i]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}

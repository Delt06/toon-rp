using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class NamedProfilingSampler : ProfilingSampler
    {
        private static readonly Dictionary<string, NamedProfilingSampler> Samplers = new();

        private NamedProfilingSampler(string name)
            : base(name) { }

        public static ProfilingSampler Get([NotNull] string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!Samplers.TryGetValue(name, out NamedProfilingSampler sampler))
            {
                Samplers[name] = sampler = new NamedProfilingSampler(name);
            }

            return sampler;
        }
    }
}
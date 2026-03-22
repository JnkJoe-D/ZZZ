using System;

namespace SkillEditor
{
    public enum HitBoxType 
    { 
        Sphere, 
        Box, 
        Capsule, 
        Sector, 
        Ring 
    }



    public enum HitFrequency 
    { 
        Once, 
        Times 
    }

    public enum TargetSortMode 
    { 
        None, 
        Closest, 
        Random 
    }
}

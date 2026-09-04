using ATEditor;
using Game.Logic;

namespace Game.Adapters
{
    public class ATParryWindowHandler : IParryWindowHandler
    {
        private readonly CharacterEntity _entity;

        public ATParryWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void SetParryWindowActive(bool active)
        {
            if (_entity != null && _entity.DataModule != null)
            {
                var parryData = _entity.DataModule.Get<ParryRuntimeData>();
                if (parryData != null)
                {
                    parryData.IsParrying = active;
                }
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Services.Sub;
using UnityEngine;

namespace FlowIoC.PoolModule.Services
{
    public partial interface IPoolService
    {
        void AutoInitializeAll();
        void InitializeAll();

        /// <summary>Fills every configured group and hands back the fills to wait on, the way InitializeGroupAsync does for one.</summary>
        Task InitializeAllAsync();

        void InitializeGroup(string groupKey);
        Task InitializeGroupAsync(string groupKey);

        CreateSubService Create { get; }
        ReturnSubService Return { get; }
        CheckSubService Check { get; }
        DestroySubService Destroy { get; }

        IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);
        T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem;

        /// <summary>
        /// The asynchronous Get, and the only one an addressable item answers: its prefab may still
        /// have to be loaded, and the synchronous Get cannot wait for that.
        /// </summary>
        Task<IPoolableItem> GetAsync(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);

        Task<T> GetAsync<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem;

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found.
        /// Each step is a file of its own, <c>IPoolService.Commands.&lt;Step&gt;.cs</c>: InitializeAll
        /// and InitializeGroup hold the sequence until the pools are filled.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}

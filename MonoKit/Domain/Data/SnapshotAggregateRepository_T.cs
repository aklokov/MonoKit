// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SnapshotAggregateRepository_T.cs" company="sgmunn">
//   (c) sgmunn 2012  
//
//   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//   documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//   the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
//   to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//   The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
//   the Software.
// 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//   THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//   CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
//   IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MonoKit.Domain.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MonoKit.Data;
    using MonoKit.Reactive.Subjects;
     
    // todo: snapshot repository needs a way to serialize complex data members so that we can still use sqlite
    // at the moment this will not handle internal state with complicated objects
    // an alternative is for the aggregate to return a snapshot that is serialized -- ie different to its internal state

    public class SnapshotAggregateRepository<T> : IAggregateRepository<T>, IObservableRepository where T : IAggregateRoot, new()
    {
        private readonly ISnapshotRepository repository;
        
        private readonly IDataModelEventBus eventBus;

        private readonly IAggregateManifestRepository manifest;

        private readonly Subject<IDataModelEvent> changes;
  
        public SnapshotAggregateRepository(ISnapshotRepository repository, IAggregateManifestRepository manifest, IDataModelEventBus eventBus)
        {
            this.repository = repository;
            this.eventBus = eventBus;
            this.manifest = manifest;
            this.changes = new Subject<IDataModelEvent>();
        }

        public T New()
        {
            return new T();
        }

        public T GetById(IUniqueIdentity id)
        {
            var snapshot = this.repository.GetById(id);
            
            if (snapshot == null)
            {
                return default(T);
            }
            
            var result = this.New();
   
            ((ISnapshotSupport)result).LoadFromSnapshot(snapshot);

            return result;
        }

        public IEnumerable<T> GetAll()
        {
            throw new NotSupportedException();
        }

        public void Save(T instance)
        {
            if (!instance.UncommittedEvents.Any())
            {
                return;
            }

            var current = this.GetById(instance.Identity);

            int expectedVersion = instance.UncommittedEvents.First().Version - 1;

            if ((current == null && expectedVersion != 0) || (current != null && current.Version != expectedVersion))
            {
                throw new ConcurrencyException();
            }
   
            var snapshot = ((ISnapshotSupport)instance).GetSnapshot() as ISnapshot;

            this.manifest.UpdateManifest(instance.Identity, expectedVersion, snapshot.Version);

            this.repository.Save(snapshot);

            // publish the snapshot save prior to publishing the events raised from it
            this.changes.OnNext(new DataModelChange(snapshot));

            this.PublishEvents(instance.UncommittedEvents);
            instance.Commit();
        }

        public void Delete(T instance)
        {
            this.repository.DeleteId(instance.Identity);
        }

        public void DeleteId(IUniqueIdentity id)
        {
            this.repository.DeleteId(id);
        }

        public void Dispose()
        {
            this.repository.Dispose();
        }

        public IObservable<IDataModelEvent> Changes
        {
            get
            {
                return this.changes;
            }
        }

        private void PublishEvents(IEnumerable<IAggregateEvent> events)
        {
            if (this.eventBus != null)
            {
                foreach (var evt in events.ToList())
                {
                    this.eventBus.Publish(evt);
                }
            }
        }
    }
}


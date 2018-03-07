using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Contracts.Common;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.Common;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph.Extensions;
using Mono.Cecil;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    // singleinstance!!
    public class TripleStore : ITripleStore
    {
        private readonly ITypeService _typeService;

        private IndexedList<string, Triple> _triplesByLeft;
        private IndexedList<string, Triple> _triplesByLeftInstructionKey;
        private IndexedList<string, Triple> _triplesByLeftObjectKeyMember;
        private IndexedList<string, Triple> _triplesByLeftInstanceOwnerKey;
        private IndexedList<string, Triple> _leftConstructorTriplesByTypeKey;


        private IndexedList<string, Triple> _triplesByRight;
        private IndexedList<string, Triple> _triplesByRightInstructionKey;
        private IndexedList<string, Triple> _triplesByRightObjectKeyMember;
        private IndexedList<string, Triple> _triplesByRightInstanceOwnerKey;
        private IndexedList<string, Triple> _objectInitializerTriplesByRightByConstructorInstructionKey;

        private HashSet<Triple> _triplesHashset;

        public TripleStore(ITypeService typeService)
        {
            _typeService = typeService;

            CleanIndexes();
        }

        public void CleanIndexes()
        {
            _triplesByLeft = new IndexedList<string, Triple>();
            _triplesByLeftInstructionKey = new IndexedList<string, Triple>();
            _triplesByLeftObjectKeyMember = new IndexedList<string, Triple>();
            _triplesByLeftInstanceOwnerKey = new IndexedList<string, Triple>();
            _leftConstructorTriplesByTypeKey = new IndexedList<string, Triple>();

            _triplesByRight = new IndexedList<string, Triple>();
            _triplesByRightInstructionKey = new IndexedList<string, Triple>();
            _triplesByRightObjectKeyMember = new IndexedList<string, Triple>();
            _triplesByRightInstanceOwnerKey = new IndexedList<string, Triple>();
            _objectInitializerTriplesByRightByConstructorInstructionKey = new IndexedList<string, Triple>();

            _triplesHashset = new HashSet<Triple>();
        }

        public void Add(Triple triple)
        {
            if (triple.From.ObjectType != ObjectType.None)
            {
                _triplesByLeft.Add(triple.From.ObjectKey, triple);
                _triplesByLeftInstructionKey.Add(triple.From.InstructionKey, triple);
                _triplesByLeftObjectKeyMember.Add(triple.From.ObjectKeyMember, triple);

                if(triple.From.ObjectKey.IndexOf(".ctor") > -1)
                {
                    _leftConstructorTriplesByTypeKey.Add(triple.From.ObjectKeyType, triple);
                }
            }

            if (!string.IsNullOrEmpty(triple.From.InstanceOwnerKey))
                _triplesByLeftInstanceOwnerKey.Add(triple.From.InstanceOwnerKey, triple);

            _triplesByRight.Add(triple.To.ObjectKey, triple);
            _triplesByRightInstructionKey.Add(triple.To.InstructionKey, triple);
            _triplesByRightObjectKeyMember.Add(triple.To.ObjectKeyMember, triple);

            if (!string.IsNullOrEmpty(triple.To.InstanceOwnerKey))
                _triplesByRightInstanceOwnerKey.Add(triple.To.InstanceOwnerKey, triple);

            if(triple.SetsObjectInitializerMember)
                _objectInitializerTriplesByRightByConstructorInstructionKey.Add(triple.ConstructorInstructionKey, triple);

            _triplesHashset.Add(triple);
        }

        public List<Triple> GetAllTriples()
        {
            return _triplesHashset.ToList();
        }

        public List<Triple> Next(Triple triple)
        {
            return _triplesByLeft.Get(triple.To.ObjectKey);
        }

        //public List<Triple> NextByLinkKey(Triple triple)
        //{
        //    return _triplesByLeftByLinkId.Get(triple.To.LinkKey);
        //}

        public List<Triple> GetFrom(string objectKey)
        {
            return _triplesByLeft.Get(objectKey);
        }

        public List<Triple> Back(Triple triple)
        {
            if (triple.From.ObjectType == ObjectType.None)
                return new List<Triple>();

            if (triple.From.OwnerTypeCategory == TypeCategory.Interface)
            {
                var concreteTriples = BackConcrete(triple);
                if (concreteTriples.Any())
                    return concreteTriples;

                return BackNonAccessibleConstructor(triple);
            }

            return _triplesByRight.Get(triple.From.ObjectKey);
        }

        private List<Triple> BackConcrete(Triple triple)
        {
            var results = new List<Triple>();
            var toMemberMatches = _triplesByRightObjectKeyMember.Get(triple.From.ObjectKeyMember);
            var typesImplementingInterface = _typeService.GetTypesImplementing(triple.From.ObjectKeyType);

            foreach (var typeImplementing in typesImplementingInterface)
            {
                var tmp = toMemberMatches.Where(x => x.To.ObjectKeyType.Equals(typeImplementing.GetKey())).ToList();
                results.AddRange(tmp);
            }

            return results;
        }

        private List<Triple> BackNonAccessibleConstructor(Triple triple)
        {
            var contructorTriples = new List<Triple>();
            var typesImplementingInterface = _typeService.GetTypesImplementing(triple.From.ObjectKeyType);
            foreach(var implementingType in typesImplementingInterface)
            {
                // this is a hack, will not work with local variables, only fields
                if (!IsMatch(implementingType, triple))
                    continue;

                var constructors = BackNonAccessibleConstructor(implementingType);
                contructorTriples.AddRange(constructors);
            }

            return contructorTriples;
        }

        private List<Triple> BackNonAccessibleConstructor(TypeReference implementingType)
        {
            var contructorTriples = new List<Triple>();

            if (implementingType.Resolve().IsInterface)
            {
                var typesImplementingInterface = _typeService.GetTypesImplementing(implementingType.FullName);
                foreach (var interfaceImplementingType in typesImplementingInterface)
                {
                    var constructors = BackNonAccessibleConstructor(interfaceImplementingType);
                    contructorTriples.AddRange(constructors);
                }
            }
            else
            {
                //var constructors = _triplesHashset.Where(x =>
                //                    x.From.ObjectType != ObjectType.None
                //                    && x.From.ObjectKeyType == implementingType.FullName
                //                    && x.From.ObjectKey.IndexOf(".ctor") > -1).ToList();

                var constructors = _leftConstructorTriplesByTypeKey.Get(implementingType.FullName).Distinct().ToList();

                contructorTriples.AddRange(constructors);
            }

            return contructorTriples;
        }

        private bool IsMatch(TypeReference implementingType, Triple triple)
        {
            if(triple == null || triple.From == null || triple.From.InstanceOwnerKey == null)
            {
                return false;
            }

            if (triple.From.InstanceOwnerKey.StartsWith("field_type"))
            {
                if (triple.From.InstanceOwnerKey.IndexOf(implementingType.FullName) > -1)
                    return true;
            }

            return false;
        }

        public List<Triple> GetTo(string objectKey)
        {
            return _triplesByRight.Get(objectKey);
        }

        public List<Triple> GetToViaInstanceOwnerKey(string instanceOwnerKey)
        {
            return _triplesByRightInstanceOwnerKey.Get(instanceOwnerKey);
        }


        public List<Triple> GetFromViaInstructionKey(string instructionKey)
        {
            return _triplesByLeftInstructionKey.Get(instructionKey);
        }

        public List<Triple> GetToViaInstructionKey(string instructionKey)
        {
            return _triplesByRightInstructionKey.Get(instructionKey);
        }

        public List<Triple> GetToViaContructorInstructionKey(string constructorInstructionKey)
        {
            return _objectInitializerTriplesByRightByConstructorInstructionKey.Get(constructorInstructionKey);
        }
    }
}

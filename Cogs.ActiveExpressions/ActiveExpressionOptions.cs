using Cogs.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cogs.ActiveExpressions
{
    /// <summary>
    /// Represents certain options governing the behavior of active expressions
    /// </summary>
    public class ActiveExpressionOptions : IEquatable<ActiveExpressionOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveExpressionOptions"/> class
        /// </summary>
        public ActiveExpressionOptions()
        {
            ConstantExpressionsListenForCollectionChanged = false;
            ConstantExpressionsListenForDictionaryChanged = false;
            DisposeConstructedObjects = true;
            DisposeStaticMethodReturnValues = true;
            MemberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged = false;
            MemberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged = false;
            PreferAsyncDisposal = true;
        }

        bool blockOnAsyncDisposal;
        bool constantExpressionsListenForCollectionChanged;
        bool constantExpressionsListenForDictionaryChanged;
        bool disposeConstructedObjects;
        readonly ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool> disposeConstructedTypes = new ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool>();
        readonly ConcurrentDictionary<MethodInfo, bool> disposeMethodReturnValues = new ConcurrentDictionary<MethodInfo, bool>();
        bool disposeStaticMethodReturnValues;
        bool isFrozen;
        bool memberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged;
        bool memberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged;
        bool preferAsyncDisposal;

        /// <summary>
        /// Gets/sets whether active expression using these options should block execution of the thread evaluating them when they must asynchronously dispose of a previous value; the default is <c>false</c>
        /// </summary>
        public bool BlockOnAsyncDisposal
        {
            get => blockOnAsyncDisposal;
            set
            {
                RequireUnfrozen();
                blockOnAsyncDisposal = value;
            }
        }

        /// <summary>
        /// Gets/sets whether constant active expressions will subscribe to <see cref="INotifyCollectionChanged.CollectionChanged" /> events of their values when present and cause re-evaluations when they occur; the default is <c>true</c>
        /// </summary>
        public bool ConstantExpressionsListenForCollectionChanged
        {
            get => constantExpressionsListenForCollectionChanged;
            set
            {
                RequireUnfrozen();
                constantExpressionsListenForCollectionChanged = value;
            }
        }

        /// <summary>
        /// Gets/sets whether constant active expressions will subscribe to <see cref="INotifyDictionaryChanged.DictionaryChanged" /> events of their values when present and cause re-evaluations when they occur; the default is <c>true</c>
        /// </summary>
        public bool ConstantExpressionsListenForDictionaryChanged
        {
            get => constantExpressionsListenForDictionaryChanged;
            set
            {
                RequireUnfrozen();
                constantExpressionsListenForDictionaryChanged = value;
            }
        }

        /// <summary>
        /// Gets/sets whether active expressions using these options should dispose of objects they have constructed when the objects are replaced or otherwise discarded; the default is <c>true</c>
        /// </summary>
        public bool DisposeConstructedObjects
        {
            get => disposeConstructedObjects;
            set
            {
                RequireUnfrozen();
                disposeConstructedObjects = value;
            }
        }

        /// <summary>
        /// Gets/sets whether active expressions using these options should dispose of objects they have received as a result of invoking static (Shared in Visual Basic) methods when the objects are replaced or otherwise discarded; the default is <c>true</c>
        /// </summary>
        public bool DisposeStaticMethodReturnValues
        {
            get => disposeStaticMethodReturnValues;
            set
            {
                RequireUnfrozen();
                disposeStaticMethodReturnValues = value;
            }
        }

        /// <summary>
        /// Gets/sets whether constant member expressions will subscribe to <see cref="INotifyCollectionChanged.CollectionChanged" /> events of their values when present and retrieved from a field of a compiler-generated type and cause re-evaluations when they occur; the default is <c>true</c>
        /// </summary>
        public bool MemberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged
        {
            get => memberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged;
            set
            {
                RequireUnfrozen();
                memberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged = value;
            }
        }

        /// <summary>
        /// Gets/sets whether constant member expressions will subscribe to <see cref="INotifyDictionaryChanged.DictionaryChanged" /> events of their values when present and retrieved from a field of a compiler-generated type and cause re-evaluations when they occur; the default is <c>true</c>
        /// </summary>
        public bool MemberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged
        {
            get => memberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged;
            set
            {
                RequireUnfrozen();
                memberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged = value;
            }
        }

        /// <summary>
        /// Gets/sets whether active expression using these options will prefer asynchronous disposal over synchronous disposal when both interfaces are implemented; the default is <c>true</c>
        /// </summary>
        public bool PreferAsyncDisposal
        {
            get => preferAsyncDisposal;
            set
            {
                RequireUnfrozen();
                preferAsyncDisposal = value;
            }
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have created of the specified type and using constructor arguments of the specified types when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="type">The type of object created</param>
        /// <param name="constuctorParameterTypes">The types of the arguments passed to the constructor, in order</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryAdd((type, new EquatableList<Type>(constuctorParameterTypes)), true);
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have created using the specified constructor when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="constructor">The constructor</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddConstructedTypeDisposal(ConstructorInfo constructor)
        {
            if (constructor is null)
                throw new ArgumentNullException(nameof(constructor));
            RequireUnfrozen();
            return disposeConstructedTypes.TryAdd((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), true);
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of invoking a constructor, operator, or method, or getting the value of a property or indexer when the objects are replaced or otherwise discarded
        /// </summary>
        /// <typeparam name="T">The type of the objects</typeparam>
        /// <param name="lambda">An expression indicating the kind of behavior that is yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddExpressionValueDisposal<T>(Expression<Func<T>> lambda) => AddExpressionValueDisposal(lambda, false);

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of invoking a constructor, operator, or method, or getting the value of a property or indexer when the objects are replaced or otherwise discarded
        /// </summary>
        /// <typeparam name="T">The type of the objects</typeparam>
        /// <param name="lambda">An expression indicating the kind of behavior that is yielding the objects</param>
        /// <param name="useGenericDefinition">Whether or not objects created by operators, methods, or property or indexer getters should be disposed regardless of generic type arguments used by the source</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddExpressionValueDisposal<T>(Expression<Func<T>> lambda, bool useGenericDefinition)
        {
            if (lambda is null)
                throw new ArgumentNullException(nameof(lambda));
            RequireUnfrozen();
            return lambda.Body switch
            {
                BinaryExpression binary => AddMethodReturnValueDisposal(binary.Method, useGenericDefinition),
                IndexExpression index => AddPropertyValueDisposal(index.Indexer, useGenericDefinition),
                NewExpression @new => useGenericDefinition ? throw new ArgumentException("using generic constructor definitions is not supported", nameof(useGenericDefinition)) : AddConstructedTypeDisposal(@new.Constructor),
                MemberExpression member when member.Member is PropertyInfo property => AddPropertyValueDisposal(property, useGenericDefinition),
                MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property => AddPropertyValueDisposal(property, useGenericDefinition),
                MethodCallExpression methodCall => AddMethodReturnValueDisposal(methodCall.Method, useGenericDefinition),
                UnaryExpression unary => AddMethodReturnValueDisposal(unary.Method, useGenericDefinition),
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of invoking a specified method when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="method">The method yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddMethodReturnValueDisposal(MethodInfo method) => AddMethodReturnValueDisposal(method, false);

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of invoking a specified method when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="method">The method yielding the objects</param>
        /// <param name="useGenericDefinition">Whether or not objects created by the method should be disposed regardless of generic type arguments used to make the method</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddMethodReturnValueDisposal(MethodInfo method, bool useGenericDefinition)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            RequireUnfrozen();
            if (useGenericDefinition)
            {
                if (!method.IsGenericMethod)
                    throw new ArgumentException("the method specified is not generic", nameof(useGenericDefinition));
                method = genericMethodToGenericMethodDefinition.GetOrAdd(method, GetGenericMethodDefinitionFromGenericMethod);
            }
            return disposeMethodReturnValues.TryAdd(method, true);
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of getting the value of a specified property when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="property">The property yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddPropertyValueDisposal(PropertyInfo property) => AddPropertyValueDisposal(property, false);

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of getting the value of a specified property when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="property">The property yielding the objects</param>
        /// <param name="useGenericDefinition">Whether or not objects created by the property getter should be disposed regardless of generic type arguments used to make the property getter</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddPropertyValueDisposal(PropertyInfo property, bool useGenericDefinition)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));
            RequireUnfrozen();
            return AddMethodReturnValueDisposal(property.GetMethod, useGenericDefinition);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object? obj) => obj is ActiveExpressionOptions other && Equals(other);

        /// <summary>
        /// Determines whether the specified options are equal to the current options
        /// </summary>
        /// <param name="other">The other options</param>
        /// <returns><c>true</c> if the specified options are equal to the current options; otherwise, <c>false</c></returns>
        public bool Equals(ActiveExpressionOptions other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            return blockOnAsyncDisposal == other.blockOnAsyncDisposal &&
                constantExpressionsListenForCollectionChanged == other.constantExpressionsListenForCollectionChanged &&
                constantExpressionsListenForDictionaryChanged == other.constantExpressionsListenForDictionaryChanged &&
                disposeConstructedObjects == other.disposeConstructedObjects &&
                disposeStaticMethodReturnValues == other.disposeStaticMethodReturnValues &&
                memberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged == other.memberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged &&
                memberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged == other.memberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged &&
                preferAsyncDisposal == other.preferAsyncDisposal &&
                disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => (key: kv.Key, value: kv.Value)).SequenceEqual(other.disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => (key: kv.Key, value: kv.Value))) &&
                disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => (key: kv.Key, value: kv.Value)).SequenceEqual(other.disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => (key: kv.Key, value: kv.Value)));
        }

        internal void Freeze() => isFrozen = true;

        /// <summary>
        /// Gets the hash code for this <see cref="ActiveExpressionOptions"/> instance
        /// </summary>
        /// <returns>The hash code for this active expression</returns>
        public override int GetHashCode()
        {
            if (ReferenceEquals(this, Default))
                return base.GetHashCode();
            Freeze();
            var objects = new List<object>()
            {
                typeof(ActiveExpressionOptions),
                blockOnAsyncDisposal,
                constantExpressionsListenForCollectionChanged,
                constantExpressionsListenForDictionaryChanged,
                disposeConstructedObjects,
                disposeStaticMethodReturnValues,
                memberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged,
                memberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged,
                preferAsyncDisposal
            };
            objects.AddRange(disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => kv.Key).Cast<object>());
            objects.AddRange(disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => kv.Key).Cast<object>());
            return new EquatableList<object>(objects).GetHashCode();
        }

        internal bool IsConstructedTypeDisposed(Type type, EquatableList<Type> constructorParameterTypes) => DisposeConstructedObjects || disposeConstructedTypes.ContainsKey((type, constructorParameterTypes));

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have created of the specified type and using constructor arguments of the specified types when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="type">The type of object created</param>
        /// <param name="constructorParameterTypes">The types of the arguments passed to the constructor, in order</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsConstructedTypeDisposed(Type type, params Type[] constructorParameterTypes) => DisposeConstructedObjects || IsConstructedTypeDisposed(type, new EquatableList<Type>(constructorParameterTypes));

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have created using the specified constructor when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="constructor">The constructor</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsConstructedTypeDisposed(ConstructorInfo constructor)
        {
            if (constructor is null)
                throw new ArgumentNullException(nameof(constructor));
            return disposeConstructedTypes.ContainsKey((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())));
        }

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have received as a result of invoking a constructor, operator, or method, or getting the value of a property or indexer when the objects are replaced or otherwise discarded
        /// </summary>
        /// <typeparam name="T">The type of the objects</typeparam>
        /// <param name="lambda">An expression indicating the kind of behavior that is yielding the objects</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsExpressionValueDisposed<T>(Expression<Func<T>> lambda)
        {
            if (lambda is null)
                throw new ArgumentNullException(nameof(lambda));
            return lambda.Body switch
            {
                BinaryExpression binary => IsMethodReturnValueDisposed(binary.Method),
                IndexExpression index => IsPropertyValueDisposed(index.Indexer),
                NewExpression @new => IsConstructedTypeDisposed(@new.Constructor),
                MemberExpression member when member.Member is PropertyInfo property => IsPropertyValueDisposed(property),
                MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property => IsPropertyValueDisposed(property),
                MethodCallExpression methodCall => IsMethodReturnValueDisposed(methodCall.Method),
                UnaryExpression unary => IsMethodReturnValueDisposed(unary.Method),
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have received as a result of invoking a specified method when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="method">The method yielding the objects</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsMethodReturnValueDisposed(MethodInfo method)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            return (method.IsStatic && DisposeStaticMethodReturnValues) || disposeMethodReturnValues.ContainsKey(method) || (method.IsGenericMethod && disposeMethodReturnValues.ContainsKey(genericMethodToGenericMethodDefinition.GetOrAdd(method, GetGenericMethodDefinitionFromGenericMethod)));
        }

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have received as a result of getting the value of a specified property when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="property">The property yielding the objects</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsPropertyValueDisposed(PropertyInfo property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));
            return IsMethodReturnValueDisposed(property.GetMethod);
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have created of the specified type and using constructor arguments of the specified types when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="type">The type of object created</param>
        /// <param name="constuctorParameterTypes">The types of the arguments passed to the constructor, in order</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryRemove((type, new EquatableList<Type>(constuctorParameterTypes)), out _);
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have created using the specified constructor when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="constructor">The constructor</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveConstructedTypeDisposal(ConstructorInfo constructor)
        {
            if (constructor is null)
                throw new ArgumentNullException(nameof(constructor));
            RequireUnfrozen();
            return disposeConstructedTypes.TryRemove((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), out var discard);
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have received as a result of invoking a constructor, operator, or method, or getting the value of a property or indexer when the objects are replaced or otherwise discarded
        /// </summary>
        /// <typeparam name="T">The type of the objects</typeparam>
        /// <param name="lambda">An expression indicating the kind of behavior that is yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveExpressionValueDisposal<T>(Expression<Func<T>> lambda)
        {
            if (lambda is null)
                throw new ArgumentNullException(nameof(lambda));
            RequireUnfrozen();
            return lambda.Body switch
            {
                BinaryExpression binary => RemoveMethodReturnValueDisposal(binary.Method),
                IndexExpression index => RemovePropertyValueDisposal(index.Indexer),
                NewExpression @new => RemoveConstructedTypeDisposal(@new.Constructor),
                MemberExpression member when member.Member is PropertyInfo property => RemovePropertyValueDisposal(property),
                MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property => RemovePropertyValueDisposal(property),
                MethodCallExpression methodCall => RemoveMethodReturnValueDisposal(methodCall.Method),
                UnaryExpression unary => RemoveMethodReturnValueDisposal(unary.Method),
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have received as a result of invoking a specified method when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="method">The method yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveMethodReturnValueDisposal(MethodInfo method)
        {
            RequireUnfrozen();
            return disposeMethodReturnValues.TryRemove(method, out _);
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have received as a result of getting the value of a specified property when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="property">The property yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemovePropertyValueDisposal(PropertyInfo property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));
            RequireUnfrozen();
            return IsMethodReturnValueDisposed(property.GetMethod);
        }

        void RequireUnfrozen()
        {
            if (isFrozen)
                throw new InvalidOperationException();
        }

        static ActiveExpressionOptions() => Default = new ActiveExpressionOptions();

        static readonly ConcurrentDictionary<MethodInfo, MethodInfo> genericMethodToGenericMethodDefinition = new ConcurrentDictionary<MethodInfo, MethodInfo>();
        static readonly ConcurrentDictionary<MethodInfo, PropertyInfo> propertyGetMethodToProperty = new ConcurrentDictionary<MethodInfo, PropertyInfo>();

        /// <summary>
        /// Gets the default active expression options, which are used in lieu of specified options when an active expression is created
        /// </summary>
        public static ActiveExpressionOptions Default { get; }

        static MethodInfo GetGenericMethodDefinitionFromGenericMethod(MethodInfo methodInfo) => methodInfo.GetGenericMethodDefinition();

        static PropertyInfo GetPropertyFromGetMethod(MethodInfo getMethod) => getMethod.DeclaringType.GetRuntimeProperties().FirstOrDefault(property => property.GetMethod == getMethod);

        /// <summary>
        /// Determines whether two active expression options are the same
        /// </summary>
        /// <param name="a">The first options to compare, or null</param>
        /// <param name="b">The second options to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator ==(ActiveExpressionOptions a, ActiveExpressionOptions b)
        {
            if (a is null)
                throw new ArgumentNullException(nameof(a));
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two active expression options are different
        /// </summary>
        /// <param name="a">The first options to compare, or null</param>
        /// <param name="b">The second options to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpressionOptions a, ActiveExpressionOptions b) => !(a == b);
    }
}

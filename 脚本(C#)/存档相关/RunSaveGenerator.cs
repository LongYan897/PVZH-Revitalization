using Frenemypillar.Script.Attributes;
using Frenemypillar.Script.RunSet;
using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Frenemypillar.Script.Save;

public static class RunSaveGenerator
{
	private static readonly Dictionary<Tuple<Type, SavedType>, List<SaveMember>> _cache = new ();

	public static DictionarySerializable ToSerializable<T>(this T target, string rootKey) where T : class
	{
		return ToSerializable(target, rootKey, SavedType.Run);
	}

	public static DictionarySerializable ToSerializable<T>(this T target, string rootKey, SavedType mode) where T : class
	{
		var members = GetMembers(target.GetType(), mode);
		var list = new List<(string key, IRunSaveProp saveProp)>();
		foreach (var m in members)
		{
			object val = m.Getter(target);
			list.Add((m.Key, Wrap(val,mode)));
		}
		return new DictionarySerializable(rootKey, list);
	}
	private static object UnwrapWithTypeHint(IRunSaveProp prop, Type fieldType, Type elementType, SavedType mode)
	{
		return prop switch
		{
			IntProp ip => ip.ValueProp,
			BoolProp bp => bp.ValueProp,
			StringProp sp => sp.ValueProp,
			UIntProp up => up.ValueProp,
			FloatProp fp => fp.ValueProp,
			LongProp lnp => lnp.ValueProp,
			ListProp lp when elementType != null
				=> RebuildGenericList(lp.ValueProp, elementType,mode),

			DictionaryProp dp when IsSerializableEntity(fieldType)
				=> RebuildEntity(dp, TryGetStoredType(dp) ?? fieldType, mode),

			ListProp lp => lp.ValueProp,
			DictionaryProp dp => dp.ValueProp,
			_ => throw new NotSupportedException($"Unknown SaveProp Type : {prop.GetType().Name}")
		};
	}

	public static void LoadFromSerializable<T>(this T target, DictionarySerializable snap) where T : class
	{
		LoadFromSerializable(target, snap, SavedType.Run);
	}

	public static void LoadFromSerializable<T>(this T target, DictionarySerializable snap, SavedType mode) where T : class
	{
		var dict = snap.Snap.ValueProp.ToDictionary(x => x.Key, x => x.SaveProp);
		var members = GetMembers(target.GetType(), mode);
		foreach (var m in members)
		{
			if (!dict.TryGetValue(m.Key, out var prop))
				continue;
			object value = UnwrapWithTypeHint(prop, m.FieldType, m.ElementType,mode);
			m.Setter(target, value);
		}
	}
	private static object RebuildEntity(DictionaryProp dictProp, Type entityType, SavedType mode)
	{
		object instance = Activator.CreateInstance(entityType)!;
		DictionarySerializable subSnap = new DictionarySerializable("", dictProp.ValueProp);
		LoadFromSerializable(instance, subSnap,mode);
		return instance;
	}

	private static object RebuildGenericList(List<IRunSaveProp> propItems, Type elementType, SavedType mode)
	{
		Type listConstruct = typeof(List<>).MakeGenericType(elementType);
		object listInstance = Activator.CreateInstance(listConstruct)!;
		var addMethod = listConstruct.GetMethod("Add")!;

		foreach (IRunSaveProp p in propItems)
		{
			object item = UnwrapWithTypeHint(p, elementType, null,mode);
			addMethod.Invoke(listInstance, new[] { item });
		}
		return listInstance;
	}
	private static Type TryGetStoredType(DictionaryProp dictProp)
	{
		var typeEntry = dictProp.ValueProp.FirstOrDefault(p => p.Key == "$type");
		if (typeEntry.SaveProp is StringProp stringProp)
		{
			return Type.GetType(stringProp.ValueProp);
		}
		return null;
	}
	private static List<SaveMember> GetMembers(Type type, SavedType mode)
	{
		var cacheKey = Tuple.Create(type, mode);
		if (_cache.TryGetValue(cacheKey, out var list))
			return list;

		var result = new List<SaveMember>();
		Type t = type;
		while (t != null && t != typeof(object))
		{
			foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				object attrObj = mode switch
				{
					SavedType.Run => f.GetCustomAttribute<RunSavedAttribute>(),
					SavedType.Battle => f.GetCustomAttribute<BattleSavedAttribute>(),
					_ => null
				}
			;
				if (attrObj == null) continue;

				string key;
				if (attrObj is RunSavedAttribute runAttr)
					key = string.IsNullOrWhiteSpace(runAttr.Key) ? f.Name : runAttr.Key;
				else if (attrObj is BattleSavedAttribute battleAttr)
					key = string.IsNullOrWhiteSpace(battleAttr.Key) ? f.Name : battleAttr.Key;
				else
					key = f.Name;

				result.Add(new SaveMember(key, o => f.GetValue(o), (o, v) => f.SetValue(o, v), f.FieldType));
			}

			foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				object attrObj = mode switch
				{
					SavedType.Run => p.GetCustomAttribute<RunSavedAttribute>(),
					SavedType.Battle => p.GetCustomAttribute<BattleSavedAttribute>(),
					_ => null
				}
			;
				if (attrObj == null || !p.CanRead || !p.CanWrite) continue;

				string key;
				if (attrObj is RunSavedAttribute runAttr)
					key = string.IsNullOrWhiteSpace(runAttr.Key) ? p.Name : runAttr.Key;
				else if (attrObj is BattleSavedAttribute battleAttr)
					key = string.IsNullOrWhiteSpace(battleAttr.Key) ? p.Name : battleAttr.Key;
				else
					key = p.Name;

				result.Add(new SaveMember(key, o => p.GetValue(o), (o, v) => p.SetValue(o, v), p.PropertyType));
			}
			t = t.BaseType;
		}
		_cache[cacheKey] = result;
		return result;
	}
	private static IRunSaveProp Wrap(object value,SavedType mode)
	{
		if (value == null)
		{
			Log.Error("Null value warning!");
			throw new NullReferenceException("Null Value When Wrap");
		}
		return value switch
		{
			int v => new IntProp(v),
			bool v => new BoolProp(v),
			long v => new LongProp(v),
			string v => new StringProp(v),
			float v => new FloatProp(v),
			uint v => new UIntProp(v),
			System.Collections.IEnumerable en when value.GetType().IsGenericType
				&& value.GetType().GetGenericTypeDefinition() == typeof(List<>)
			=> BuildListProp(en,mode),

			_ when IsSerializableEntity(value.GetType())
				=> BuildEntityDictProp(value,mode),

			List<IRunSaveProp> v => new ListProp(v),
			List<(string key, IRunSaveProp saveProp)> v => new DictionaryProp(v),
			_ => throw new NotSupportedException($"Unknown SaveProp Type : {value.GetType().Name}")
		};
	}
	private static DictionaryProp BuildEntityDictProp(object entity,SavedType mode)
	{
		Type type = entity.GetType();
		var members = GetMembers(type,mode);
		var itemList = new List<(string key, IRunSaveProp saveProp)>();
		itemList.Add(("$type", new StringProp(type.FullName)));
		foreach (var m in members)
		{
			object val = m.Getter(entity);
			itemList.Add((m.Key, Wrap(val,mode)));
		}
		return new DictionaryProp(itemList);
	}

	private static ListProp BuildListProp(System.Collections.IEnumerable enumerable, SavedType mode)
	{
		var items = new List<IRunSaveProp>();
		foreach (var item in enumerable)
		{
			items.Add(Wrap(item,mode));
		}
		return new ListProp(items);
	}

	private static bool IsSerializableEntity(Type type)
	{
		Type scan = type;
		while (scan != null && scan != typeof(object))
		{
			foreach (FieldInfo f in scan.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (f.GetCustomAttribute<RunSavedAttribute>() != null || f.GetCustomAttribute<BattleSavedAttribute>() != null)
					return true;
			}
			foreach (PropertyInfo p in scan.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (p.GetCustomAttribute<RunSavedAttribute>() != null || p.GetCustomAttribute<BattleSavedAttribute>() != null)
					return true;
			}
			scan = scan.BaseType;
		}
		return false;
	}
	private sealed class SaveMember
	{
		public string Key { get; }
		public Func<object, object> Getter { get; }
		public Action<object, object> Setter { get; }
		public Type FieldType { get; }
		public Type ElementType { get; }

		public SaveMember(string key, Func<object, object> getter, Action<object, object> setter, Type fieldType)
		{
			Key = key;
			Getter = getter;
			Setter = setter;
			FieldType = fieldType;
			if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
			{
				ElementType = fieldType.GetGenericArguments()[0];
			}
			else
			{
				ElementType = null;
			}
		}
	}

}

//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml.Schema;

namespace System.Xaml
{
	public class XamlType : IEquatable<XamlType>
	{
		public XamlType (Type underlyingType, XamlSchemaContext schemaContext)
			: this (underlyingType, schemaContext, null)
		{
		}

		static readonly Type [] predefined_types = {
				typeof (XData), typeof (Uri), typeof (TimeSpan), typeof (PropertyDefinition), typeof (MemberDefinition), typeof (Reference)
			};

		public XamlType (Type underlyingType, XamlSchemaContext schemaContext, XamlTypeInvoker invoker)
			: this (schemaContext, invoker)
		{
			if (underlyingType == null)
				throw new ArgumentNullException ("underlyingType");
			type = underlyingType;
			underlying_type = type;

			Name = type.GetXamlName ();
			// FIXME: remove this hack
			if (Type.GetTypeCode (type) == TypeCode.Object && type != typeof (object)) {
				if (predefined_types.Contains (type) || typeof (MarkupExtension).IsAssignableFrom (type) && type.Assembly == typeof (XamlType).Assembly)
					PreferredXamlNamespace = XamlLanguage.Xaml2006Namespace;
				else
					PreferredXamlNamespace = String.Format ("clr-namespace:{0};assembly={1}", type.Namespace, type.Assembly.GetName ().Name);
			}
			else
				PreferredXamlNamespace = XamlLanguage.Xaml2006Namespace;
		}

		public XamlType (string unknownTypeNamespace, string unknownTypeName, IList<XamlType> typeArguments, XamlSchemaContext schemaContext)
			: this (schemaContext, null)
		{
			if (unknownTypeNamespace == null)
				throw new ArgumentNullException ("unknownTypeNamespace");
			if (unknownTypeName == null)
				throw new ArgumentNullException ("unknownTypeName");
			if (schemaContext == null)
				throw new ArgumentNullException ("schemaContext");

			type = typeof (object);
			Name = unknownTypeName;
			PreferredXamlNamespace = unknownTypeNamespace;
			TypeArguments = typeArguments != null && typeArguments.Count == 0 ? null : typeArguments;
		}

		protected XamlType (string typeName, IList<XamlType> typeArguments, XamlSchemaContext schemaContext)
			: this (String.Empty, typeName, typeArguments, schemaContext)
		{
		}

		XamlType (XamlSchemaContext schemaContext, XamlTypeInvoker invoker)
		{
			if (schemaContext == null)
				throw new ArgumentNullException ("schemaContext");
			SchemaContext = schemaContext;
			this.invoker = invoker ?? new XamlTypeInvoker (this);
		}

		Type type, underlying_type;

		// populated properties
		XamlType base_type;
		XamlTypeInvoker invoker;

		internal EventHandler<XamlSetMarkupExtensionEventArgs> SetMarkupExtensionHandler {
			get { return LookupSetMarkupExtensionHandler (); }
		}

		internal EventHandler<XamlSetTypeConverterEventArgs> SetTypeConverterHandler {
			get { return LookupSetTypeConverterHandler (); }
		}

		public IList<XamlType> AllowedContentTypes {
			get { return LookupAllowedContentTypes (); }
		}

		public XamlType BaseType {
			get { return LookupBaseType (); }
		}

		public bool ConstructionRequiresArguments {
			get { return LookupConstructionRequiresArguments (); }
		}

		public XamlMember ContentProperty {
			get { return LookupContentProperty (); }
		}

		public IList<XamlType> ContentWrappers {
			get { return LookupContentWrappers (); }
		}

		public XamlValueConverter<XamlDeferringLoader> DeferringLoader {
			get { return LookupDeferringLoader (); }
		}

		public XamlTypeInvoker Invoker {
			get { return LookupInvoker (); }
		}

		public bool IsAmbient {
			get { return LookupIsAmbient (); }
		}
		public bool IsArray {
			get { return type.IsArray; }
		}
		public bool IsCollection {
			// it somehow treats array as not a collection...
			get { return !type.IsArray && type.ImplementsAnyInterfacesOf (typeof (ICollection), typeof (ICollection<>)); }
		}

		public bool IsConstructible {
			get { return LookupIsConstructible (); }
		}

		public bool IsDictionary {
			get { return type.ImplementsAnyInterfacesOf (typeof (IDictionary), typeof (IDictionary<,>)); }
		}

		public bool IsGeneric {
			get { return type.IsGenericType; }
		}

		public bool IsMarkupExtension {
			get { return LookupIsMarkupExtension (); }
		}
		public bool IsNameScope {
			get { return LookupIsNameScope (); }
		}
		public bool IsNameValid {
			get { return XamlLanguage.IsValidXamlName (Name); }
		}

		public bool IsNullable {
			get { return LookupIsNullable (); }
		}

		public bool IsPublic {
			get { return LookupIsPublic (); }
		}

		public bool IsUnknown {
			get { return LookupIsUnknown (); }
		}

		public bool IsUsableDuringInitialization {
			get { return LookupUsableDuringInitialization (); }
		}

		public bool IsWhitespaceSignificantCollection {
			get { return LookupIsWhitespaceSignificantCollection (); }
		}

		public bool IsXData {
			get { return LookupIsXData (); }
		}

		public XamlType ItemType {
			get { return LookupItemType (); }
		}

		public XamlType KeyType {
			get { return LookupKeyType (); }
		}

		public XamlType MarkupExtensionReturnType {
			get { return LookupMarkupExtensionReturnType (); }
		}

		public string Name { get; private set; }

		public string PreferredXamlNamespace { get; private set; }

		public XamlSchemaContext SchemaContext { get; private set; }

		public bool TrimSurroundingWhitespace {
			get { return LookupTrimSurroundingWhitespace (); }
		}

		public IList<XamlType> TypeArguments { get; private set; }

		public XamlValueConverter<TypeConverter> TypeConverter {
			get { return LookupTypeConverter (); }
		}

		public Type UnderlyingType {
			get { return LookupUnderlyingType (); }
		}

		public XamlValueConverter<ValueSerializer> ValueSerializer {
			get { return LookupValueSerializer (); }
		}

		public static bool operator == (XamlType left, XamlType right)
		{
			return IsNull (left) ? IsNull (right) : left.Equals (right);
		}

		static bool IsNull (XamlType a)
		{
			return Object.ReferenceEquals (a, null);
		}

		public static bool operator != (XamlType left, XamlType right)
		{
			return !(left == right);
		}
		
		public bool Equals (XamlType other)
		{
			return !IsNull (other) &&
				UnderlyingType == other.UnderlyingType &&
				Name == other.Name &&
				PreferredXamlNamespace == other.PreferredXamlNamespace &&
				CompareTypes (TypeArguments, other.TypeArguments);
		}

		static bool CompareTypes (IList<XamlType> a1, IList<XamlType> a2)
		{
			if (a1 == null)
				return a2 == null;
			if (a2 == null)
				return false;
			if (a1.Count != a2.Count)
				return false;
			for (int i = 0; i < a1.Count; i++)
				if (a1 [i] != a2 [i])
					return false;
			return true;
		}

		public override bool Equals (object obj)
		{
			var a = obj as XamlType;
			return Equals (a);
		}
		
		public override int GetHashCode ()
		{
			if (UnderlyingType != null)
				return UnderlyingType.GetHashCode ();
			int x = Name.GetHashCode () << 7 + PreferredXamlNamespace.GetHashCode ();
			if (TypeArguments != null)
				foreach (var t in TypeArguments)
					x = t.GetHashCode () + x << 5;
			return x;
		}

		public override string ToString ()
		{
			return UnderlyingType != null ? UnderlyingType.ToString () : Name;
		}

		public virtual bool CanAssignTo (XamlType xamlType)
		{
			throw new NotImplementedException ();
		}

		public XamlMember GetAliasedProperty (XamlDirective directive)
		{
			return LookupAliasedProperty (directive);
		}

		public ICollection<XamlMember> GetAllAttachableMembers ()
		{
			return new List<XamlMember> (LookupAllAttachableMembers ());
		}

		public ICollection<XamlMember> GetAllMembers ()
		{
			return new List<XamlMember> (LookupAllMembers ());
		}

		public XamlMember GetAttachableMember (string name)
		{
			return LookupAttachableMember (name);
		}

		public XamlMember GetMember (string name)
		{
			return LookupMember (name, false);
		}

		public IList<XamlType> GetPositionalParameters (int parameterCount)
		{
			return LookupPositionalParameters (parameterCount);
		}

		public virtual IList<string> GetXamlNamespaces ()
		{
			throw new NotImplementedException ();
		}

		// lookups

		protected virtual XamlMember LookupAliasedProperty (XamlDirective directive)
		{
			throw new NotImplementedException ();
		}
		protected virtual IEnumerable<XamlMember> LookupAllAttachableMembers ()
		{
			throw new NotImplementedException ();
		}
		protected virtual IEnumerable<XamlMember> LookupAllMembers ()
		{
			throw new NotImplementedException ();
		}
		protected virtual IList<XamlType> LookupAllowedContentTypes ()
		{
			throw new NotImplementedException ();
		}
		protected virtual XamlMember LookupAttachableMember (string name)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		protected virtual XamlType LookupBaseType ()
		{
			if (base_type == null) {
				if (UnderlyingType == null)
					// FIXME: probably something advanced is needed here.
					base_type = new XamlType (typeof (object), SchemaContext, Invoker);
				else
					base_type = type.BaseType == null || type.BaseType == typeof (object) ? null : new XamlType (type.BaseType, SchemaContext, Invoker);
			}
			return base_type;
		}

		protected virtual XamlCollectionKind LookupCollectionKind ()
		{
			throw new NotImplementedException ();
		}

		protected virtual bool LookupConstructionRequiresArguments ()
		{
			if (UnderlyingType == null)
				return false;

			// not sure if it is required, but TypeDefinition and MemberDefinition return true while they are abstract and it makes no sense.
			if (UnderlyingType.IsAbstract)
				return true;

			// FIXME: probably some primitive types are treated as special.
			switch (Type.GetTypeCode (UnderlyingType)) {
			case TypeCode.String:
				return true;
			case TypeCode.Object:
				if (UnderlyingType == typeof (TimeSpan))
					return false;
				break;
			default:
				return false;
			}

			return UnderlyingType.GetConstructor (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) == null;
		}

		protected virtual XamlMember LookupContentProperty ()
		{
			var a = this.GetCustomAttribute<ContentPropertyAttribute> ();
			return a != null && a.Name != null ? GetMember (a.Name) : null;
		}
		protected virtual IList<XamlType> LookupContentWrappers ()
		{
			throw new NotImplementedException ();
		}
		protected virtual ICustomAttributeProvider LookupCustomAttributeProvider ()
		{
			throw new NotImplementedException ();
		}
		protected virtual XamlValueConverter<XamlDeferringLoader> LookupDeferringLoader ()
		{
			throw new NotImplementedException ();
		}

		protected virtual XamlTypeInvoker LookupInvoker ()
		{
			return invoker;
		}

		protected virtual bool LookupIsAmbient ()
		{
			return this.GetCustomAttribute<AmbientAttribute> () != null;
		}

		protected virtual bool LookupIsConstructible ()
		{
			// see spec. 5.2.
			if (IsArray) // x:Array
				return false;
			if (type == typeof (XamlType)) // x:XamlType
				return false;
			// FIXME: handle x:XamlEvent
			if (IsMarkupExtension)
				return false;
			// FIXME: handle x:Code
			// FIXME: commented out.
			//if (IsXData)
			//	return false;

			// FIXME: this check is extraneous to spec. 5.2.
			if (ConstructionRequiresArguments)
				return false;

			return true;
		}

		protected virtual bool LookupIsMarkupExtension ()
		{
			return typeof (MarkupExtension).IsAssignableFrom (UnderlyingType);
		}

		protected virtual bool LookupIsNameScope ()
		{
			return typeof (INameScope).IsAssignableFrom (UnderlyingType);
		}

		protected virtual bool LookupIsNullable ()
		{
			return !type.IsValueType || type.ImplementsInterface (typeof (Nullable<>));
		}

		protected virtual bool LookupIsPublic ()
		{
			return underlying_type == null || underlying_type.IsPublic || underlying_type.IsNestedPublic;
		}

		protected virtual bool LookupIsUnknown ()
		{
			return UnderlyingType == null;
		}

		protected virtual bool LookupIsWhitespaceSignificantCollection ()
		{
			// probably for unknown types, it should preserve whitespaces.
			return IsUnknown || this.GetCustomAttribute<WhitespaceSignificantCollectionAttribute> () != null;
		}

		protected virtual bool LookupIsXData ()
		{
			// huh? XamlLanguage.XData.IsXData returns false(!)
			// return typeof (XData).IsAssignableFrom (UnderlyingType);
			return false;
		}

		protected virtual XamlType LookupItemType ()
		{
			if (IsArray)
				return new XamlType (type.GetElementType (), SchemaContext);
			if (!IsCollection)
				return null;
			if (!IsGeneric)
				return new XamlType (typeof (object), SchemaContext);
			return new XamlType (type.GetGenericArguments () [0], SchemaContext);
		}

		protected virtual XamlType LookupKeyType ()
		{
			if (!IsDictionary)
				return null;
			if (!IsGeneric)
				return new XamlType (typeof (object), SchemaContext);
			return new XamlType (type.GetGenericArguments () [0], SchemaContext);
		}

		protected virtual XamlType LookupMarkupExtensionReturnType ()
		{
			var a = this.GetCustomAttribute<MarkupExtensionReturnTypeAttribute> ();
			return a != null ? new XamlType (a.ReturnType, SchemaContext) : null;
		}

		protected virtual XamlMember LookupMember (string name, bool skipReadOnlyCheck)
		{
			var pi = UnderlyingType.GetProperty (name);
			if (pi != null && (skipReadOnlyCheck || pi.CanWrite))
				return new XamlMember (pi, SchemaContext);
			var ei = UnderlyingType.GetEvent (name);
			if (ei != null)
				return new XamlMember (ei, SchemaContext);
			return null;
		}

		protected virtual IList<XamlType> LookupPositionalParameters (int parameterCount)
		{
			throw new NotImplementedException ();
		}

		BindingFlags flags_get_static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		protected virtual EventHandler<XamlSetMarkupExtensionEventArgs> LookupSetMarkupExtensionHandler ()
		{
			var a = this.GetCustomAttribute<XamlSetMarkupExtensionAttribute> ();
			if (a == null)
				return null;
			var mi = type.GetMethod (a.XamlSetMarkupExtensionHandler, flags_get_static);
			if (mi == null)
				throw new ArgumentException ("Binding to XamlSetMarkupExtensionHandler failed");
			return (EventHandler<XamlSetMarkupExtensionEventArgs>) Delegate.CreateDelegate (typeof (EventHandler<XamlSetMarkupExtensionEventArgs>), mi);
		}

		protected virtual EventHandler<XamlSetTypeConverterEventArgs> LookupSetTypeConverterHandler ()
		{
			var a = this.GetCustomAttribute<XamlSetTypeConverterAttribute> ();
			if (a == null)
				return null;
			var mi = type.GetMethod (a.XamlSetTypeConverterHandler, flags_get_static);
			if (mi == null)
				throw new ArgumentException ("Binding to XamlSetTypeConverterHandler failed");
			return (EventHandler<XamlSetTypeConverterEventArgs>) Delegate.CreateDelegate (typeof (EventHandler<XamlSetTypeConverterEventArgs>), mi);
		}

		protected virtual bool LookupTrimSurroundingWhitespace ()
		{
			return this.GetCustomAttribute<TrimSurroundingWhitespaceAttribute> () != null;
		}

		protected virtual XamlValueConverter<TypeConverter> LookupTypeConverter ()
		{
			throw new NotImplementedException ();
		}

		protected virtual Type LookupUnderlyingType ()
		{
			return underlying_type;
		}

		protected virtual bool LookupUsableDuringInitialization ()
		{
			var a = this.GetCustomAttribute<UsableDuringInitializationAttribute> ();
			return a != null && a.Usable;
		}

		protected virtual XamlValueConverter<ValueSerializer> LookupValueSerializer ()
		{
			throw new NotImplementedException ();
		}
	}
}

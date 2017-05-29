﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using OpenQA.Selenium;

namespace Atata
{
    /// <summary>
    /// Represents the list of controls of <typeparamref name="TItem"/> type.
    /// </summary>
    /// <typeparam name="TItem">The type of the item control.</typeparam>
    /// <typeparam name="TOwner">The type of the owner page object.</typeparam>
    public class ControlList<TItem, TOwner> : UIComponentPart<TOwner>, IDataProvider<IEnumerable<TItem>, TOwner>, IEnumerable<TItem>, ISupportsMetadata
        where TItem : Control<TOwner>
        where TOwner : PageObject<TOwner>
    {
        private FindAttribute itemFindAttribute;

        protected ControlDefinitionAttribute ItemDefinition => (ControlDefinitionAttribute)Metadata.ComponentDefinitonAttribute;

        protected FindAttribute ItemFindAttribute => itemFindAttribute ?? (itemFindAttribute = ResolveItemFindAttribute());

        /// <summary>
        /// Gets the verification provider that gives a set of verification extension methods.
        /// </summary>
        public DataVerificationProvider<IEnumerable<TItem>, TOwner> Should => new DataVerificationProvider<IEnumerable<TItem>, TOwner>(this);

        /// <summary>
        /// Gets the <see cref="DataProvider{TData, TOwner}"/> instance for the controls count.
        /// </summary>
        public DataProvider<int, TOwner> Count => Component.GetOrCreateDataProvider($"{ItemDefinition.ComponentTypeName} count", GetCount);

        /// <summary>
        /// Gets the <see cref="DataProvider{TData, TOwner}"/> instance for the controls contents.
        /// </summary>
        public DataProvider<IEnumerable<string>, TOwner> Contents => Component.GetOrCreateDataProvider($"{ItemDefinition.ComponentTypeName} contents", GetContents);

        UIComponent IDataProvider<IEnumerable<TItem>, TOwner>.Component => (UIComponent)Component;

        protected string ProviderName => $"{ItemDefinition.ComponentTypeName} list";

        string IDataProvider<IEnumerable<TItem>, TOwner>.ProviderName => ProviderName;

        TOwner IDataProvider<IEnumerable<TItem>, TOwner>.Owner => Component.Owner;

        TermOptions IDataProvider<IEnumerable<TItem>, TOwner>.ValueTermOptions { get; }

        IEnumerable<TItem> IDataProvider<IEnumerable<TItem>, TOwner>.Value => GetAll();

        UIComponentMetadata ISupportsMetadata.Metadata
        {
            get { return Metadata; }
            set { Metadata = value; }
        }

        public UIComponentMetadata Metadata { get; private set; }

        Type ISupportsMetadata.ComponentType
        {
            get { return typeof(TItem); }
        }

        /// <summary>
        /// Gets the control at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get.</param>
        /// <returns>The item at the specified index.</returns>
        public TItem this[int index]
        {
            get
            {
                index.CheckIndexNonNegative();

                return GetItemByIndex(index);
            }
        }

        /// <summary>
        /// Gets the control that matches the conditions defined by the specified predicate expression.
        /// </summary>
        /// <param name="predicateExpression">The predicate expression to test each item.</param>
        /// <returns>The first item that matches the conditions of the specified predicate.</returns>
        public TItem this[Expression<Func<TItem, bool>> predicateExpression]
        {
            get
            {
                predicateExpression.CheckNotNull(nameof(predicateExpression));

                string itemName = UIComponentResolver.ResolveControlName<TItem, TOwner>(predicateExpression);

                return GetItem(itemName, predicateExpression);
            }
        }

        private FindAttribute ResolveItemFindAttribute()
        {
            FindAttribute findAttribute = new FindControlListItemAttribute();
            findAttribute.Properties.Metadata = Metadata;
            return findAttribute;
        }

        /// <summary>
        /// Gets the controls count.
        /// </summary>
        /// <returns>The count of controls.</returns>
        protected virtual int GetCount()
        {
            By itemBy = CreateItemBy();
            return GetItemElements(itemBy.AtOnce()).Count;
        }

        /// <summary>
        /// Gets the controls contents.
        /// </summary>
        /// <returns>The contents of controls.</returns>
        protected virtual IEnumerable<string> GetContents()
        {
            return GetAll().Select(x => (string)x.Content);
        }

        protected TItem GetItemByIndex(int index)
        {
            string itemName = OrdinalizeNumber(index + 1);

            return CreateItem(itemName, new FindByIndexAttribute(index));
        }

        protected TItem GetItemByInnerXPath(string itemName, string xPath)
        {
            return CreateItem(itemName, new FindByInnerXPathAttribute(xPath));
        }

        protected virtual TItem GetItem(string name, Expression<Func<TItem, bool>> predicateExpression)
        {
            By itemBy = CreateItemBy();
            var predicate = predicateExpression.Compile();

            ControlListScopeLocator scopeLocator = new ControlListScopeLocator(options =>
            {
                return GetItemElements(itemBy.With(options).SafelyAtOnce()).
                    Where(element => predicate(CreateItem(new DefinedScopeLocator(element), name)));
            });

            return CreateItem(scopeLocator, name);
        }

        /// <summary>
        /// Searches for the item that matches the conditions defined by the specified predicate expression
        /// and returns the zero-based index of the first occurrence.
        /// </summary>
        /// <param name="predicateExpression">The predicate expression to test each item.</param>
        /// <returns>The zero-based index of the first occurrence of item, if found; otherwise, –1.</returns>
        public DataProvider<int, TOwner> IndexOf(Expression<Func<TItem, bool>> predicateExpression)
        {
            predicateExpression.CheckNotNull(nameof(predicateExpression));

            string itemName = UIComponentResolver.ResolveControlName<TItem, TOwner>(predicateExpression);

            return Component.GetOrCreateDataProvider($"index of \"{itemName}\" {ItemDefinition.ComponentTypeName}", () =>
            {
                return IndexOf(itemName, predicateExpression);
            });
        }

        protected virtual int IndexOf(string name, Expression<Func<TItem, bool>> predicateExpression)
        {
            By itemBy = CreateItemBy();
            var predicate = predicateExpression.Compile();

            return GetItemElements(itemBy.SafelyAtOnce()).
                Select((element, index) => new { Element = element, Index = index }).
                Where(x => predicate(CreateItem(new DefinedScopeLocator(x.Element), name))).
                Select(x => (int?)x.Index).
                FirstOrDefault() ?? -1;
        }

        protected virtual By CreateItemBy()
        {
            string outerXPath = ItemFindAttribute.OuterXPath ?? ".//";

            By by = By.XPath($"{outerXPath}{ItemDefinition.ScopeXPath}").OfKind(ItemDefinition.ComponentTypeName);

            // TODO: Review/remake this Visibility processing.
            if (ItemFindAttribute.Visibility == Visibility.Any)
                by = by.OfAnyVisibility();
            else if (ItemFindAttribute.Visibility == Visibility.Hidden)
                by = by.Hidden();

            return by;
        }

        protected virtual TItem CreateItem(string name, params Attribute[] attributes)
        {
            var itemAttributes = attributes?.Concat(GetItemDeclaredAttributes()) ?? GetItemDeclaredAttributes();

            return Component.Controls.Create<TItem>(name, itemAttributes.ToArray());
        }

        protected TItem CreateItem(IScopeLocator scopeLocator, string name)
        {
            TItem item = CreateItem(name);
            item.ScopeLocator = scopeLocator;

            return item;
        }

        protected virtual IEnumerable<Attribute> GetItemDeclaredAttributes()
        {
            return Metadata.DeclaredAttributes.Where(x => !(x is FindAttribute));
        }

        private string OrdinalizeNumber(int number)
        {
            string ending = "th";

            int tensDigit = (number % 100) / 10;

            if (tensDigit != 1)
            {
                int unitDigit = number % 10;

                ending = unitDigit == 1 ? "st"
                    : unitDigit == 2 ? "nd"
                    : unitDigit == 3 ? "rd"
                    : ending;
            }

            return $"{number}{ending}";
        }

        /// <summary>
        /// Selects the specified data (property) set of each control. Data can be a sub-control, an instance of <see cref="DataProvider{TData, TOwner}"/>, etc.
        /// </summary>
        /// <typeparam name="TData">The type of the data.</typeparam>
        /// <param name="selector">The data selector.</param>
        /// <returns>An instance of <see cref="DataProvider{TData, TOwner}"/>.</returns>
        public DataProvider<IEnumerable<TData>, TOwner> SelectData<TData>(Expression<Func<TItem, TData>> selector)
        {
            string dataPathName = ControlNameExpressionStringBuilder.ExpressionToString(selector);

            return Component.GetOrCreateDataProvider(
                $"\"{dataPathName}\" {ProviderName}",
                () => GetAll().Select(selector.Compile()));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<TItem> GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }

        protected virtual IEnumerable<TItem> GetAll()
        {
            By itemBy = CreateItemBy();

            return GetItemElements(itemBy.AtOnce()).
                Select((element, index) => CreateItem(new DefinedScopeLocator(element), OrdinalizeNumber(index + 1))).
                ToArray();
        }

        protected ReadOnlyCollection<IWebElement> GetItemElements(By itemBy)
        {
            return ItemFindAttribute.ScopeSource.GetScopeElementUsingParent(Component).GetAll(itemBy);
        }

        IEnumerable<TItem> IDataProvider<IEnumerable<TItem>, TOwner>.Get() => GetAll();
    }
}

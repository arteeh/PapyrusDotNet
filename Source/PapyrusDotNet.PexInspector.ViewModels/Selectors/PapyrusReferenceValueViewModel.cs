using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using PapyrusDotNet.PapyrusAssembly;
using PapyrusDotNet.PapyrusAssembly.Extensions;
using PapyrusDotNet.PexInspector.ViewModels.Implementations;

namespace PapyrusDotNet.PexInspector.ViewModels.Selectors
{
    public class PapyrusReferenceValueViewModel : ViewModelBase
    {
        private readonly List<PapyrusAssemblyDefinition> loadedAssemblies;
        private readonly PapyrusTypeDefinition currentType;
        private readonly PapyrusMethodDefinition currentMethod;
        private readonly OpCodeArgumentDescription desc;

        public PapyrusReferenceValueViewModel(List<PapyrusAssemblyDefinition> loadedAssemblies, PapyrusTypeDefinition currentType, PapyrusMethodDefinition currentMethod, OpCodeArgumentDescription desc)
        {
            this.loadedAssemblies = loadedAssemblies;
            this.currentType = currentType;
            this.currentMethod = currentMethod;
            this.desc = desc;
            if (currentMethod != null)
            {
                ReferenceCollection = new ObservableCollection<PapyrusMemberReference>(currentMethod.GetVariables());
            }
            ComboBoxItems = new ObservableCollection<FrameworkElement>(CreateComboBoxItems());
            SelectedReferenceType = ComboBoxItems.First() as ComboBoxItem;
            ReferenceSelectionVisible = Visibility.Visible;
        }

        private ObservableCollection<FrameworkElement> comboBoxItems;
        public ObservableCollection<FrameworkElement> ComboBoxItems
        {
            get { return comboBoxItems; }
            set { Set(ref comboBoxItems, value); }
        }

        private List<FrameworkElement> CreateComboBoxItems()
        {
            var elements = new List<FrameworkElement>();
            elements.Add(new ComboBoxItem { Content = "Parameter" });
            elements.Add(new ComboBoxItem { Content = "Variable" });
            elements.Add(new ComboBoxItem { Content = "Field" });
            //if (desc == null || desc.Constraints.Length == 0)
            {
                elements.Add(new ComboBoxItem { Content = "Self" });
                elements.Add(new ComboBoxItem { Content = "SelfRef" });
            }
            return elements;
        }

        public PapyrusMemberReference SelectedReference
        {
            get { return selectedReference; }
            set { Set(ref selectedReference, value); }
        }

        public ComboBoxItem SelectedReferenceType
        {
            get { return selectedReferenceType; }
            set
            {
                if (Set(ref selectedReferenceType, value))
                {
                    UpdateReferenceCollection(value);
                }
            }
        }

        private void UpdateReferenceCollection(ComboBoxItem value)
        {
            if (currentMethod == null) return;
            var tar = value.Content.ToString().ToLower();
            ReferenceSelectionVisible = Visibility.Visible;
            if (tar == "variable")
            {
                var papyrusVariableReferences = currentMethod.GetVariables();
                papyrusVariableReferences.ForEach(i => i.ValueType = PapyrusPrimitiveType.Reference);
                ReferenceCollection = new ObservableCollection<PapyrusMemberReference>(Filter(papyrusVariableReferences));
            }
            else if (tar == "parameter")
            {
                var papyrusParameterDefinitions = currentMethod.Parameters;
                ReferenceCollection = new ObservableCollection<PapyrusMemberReference>(Filter(papyrusParameterDefinitions));
            }
            else if (tar == "field")
            {
                var papyrusFieldDefinitions = currentType.Fields.ToList();
                ReferenceCollection = new ObservableCollection<PapyrusMemberReference>(Filter(papyrusFieldDefinitions));
            }
            else if (tar == "self")
            {
                if (currentType.Assembly != null)
                {
                    SelectedReference = new PapyrusVariableReference
                    {
                        Value = "Self",
                        ValueType = PapyrusPrimitiveType.Reference
                    };
                }
                ReferenceSelectionVisible = Visibility.Collapsed;
            }
            else if (tar == "selfref")
            {
                if (currentType.Assembly != null)
                {
                    SelectedReference = new PapyrusVariableReference
                    {
                        Value = "SelfRef",
                        ValueType = PapyrusPrimitiveType.Reference
                    };
                }
                ReferenceSelectionVisible = Visibility.Collapsed;
            }
        }


        private IEnumerable<PapyrusMemberReference> Filter(List<PapyrusParameterDefinition> collection)
        {
            if (desc == null || desc.Constraints.Length == 0) return collection;
            var result = new List<PapyrusMemberReference>();
            foreach (var constraint in desc.Constraints)
            {
                var type = constraint.ToString().ToLower();
                if (type == "boolean") type = "bool";
                if (type == "integer") type = "int";
                var range = collection.Where(i => i.TypeName.Value.ToLower() == type);
                result.AddRange(range);
            }
            return result;
        }


        private IEnumerable<PapyrusMemberReference> Filter(List<PapyrusFieldDefinition> collection)
        {
            if (desc == null || desc.Constraints.Length == 0) return collection;
            var result = new List<PapyrusMemberReference>();
            foreach (var constraint in desc.Constraints)
            {
                var type = constraint.ToString().ToLower();
                if (type == "boolean") type = "bool";
                if (type == "integer") type = "int";
                var range = collection.Where(i => i.TypeName.ToLower() == type);
                result.AddRange(range);
            }
            return result;
        }

        private IEnumerable<PapyrusMemberReference> Filter(List<PapyrusVariableReference> collection)
        {
            if (desc == null || desc.Constraints.Length == 0) return collection;
            var result = new List<PapyrusMemberReference>();
            foreach (var constraint in desc.Constraints)
            {
                var type = constraint.ToString().ToLower();
                if (type == "boolean") type = "bool";
                if (type == "integer") type = "int";
                var range = collection.Where(i => i.TypeName.Value.ToLower() == type);
                result.AddRange(range);
            }
            return result;
        }


        public ObservableCollection<PapyrusMemberReference> ReferenceCollection
        {
            get { return referenceCollection; }
            set { Set(ref referenceCollection, value); }
        }

        public string SelectedReferenceName { get; set; }

        public Visibility ReferenceSelectionVisible
        {
            get { return referenceSelectionVisible; }
            set { Set(ref referenceSelectionVisible, value); }
        }

        public static PapyrusReferenceValueViewModel DesignInstance = designInstance ??
                                                         (designInstance =
                                                             new PapyrusReferenceValueViewModel(null, null, null, null));

        private static PapyrusReferenceValueViewModel designInstance;
        private PapyrusMemberReference selectedReference;
        private ComboBoxItem selectedReferenceType;
        private ObservableCollection<PapyrusMemberReference> referenceCollection;
        private Visibility referenceSelectionVisible;
    }
}
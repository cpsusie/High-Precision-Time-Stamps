﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UnitTests {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class binary_operations {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal binary_operations() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("UnitTests.binary_operations", typeof(binary_operations).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Compare;ffffffffffffffff	7fffffffffffffff	;ffffffffffffffff	7fffffffffffffff	;0000000000000000	0000000000000000	;
        ///Compare;ffffffffffffffff	7fffffffffffffff	;fffffffffffffffe	7fffffffffffffff	;0000000000000001	0000000000000000	;
        ///Compare;ffffffffffffffff	7fffffffffffffff	;0000000000000000	8000000000000000	;0000000000000001	0000000000000000	;
        ///Compare;ffffffffffffffff	7fffffffffffffff	;0000000000000001	8000000000000000	;0000000000000001	0000000000000000	;
        ///Compare;ffffffffffffffff	7fffffffffffffff	;7ffffffff [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string comp_edge_ops {
            get {
                return ResourceManager.GetString("comp_edge_ops", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiply;958e83d29739c5fe	ffffffffffffffff	;000000000012a065	0000000000000000	;586c42a9b165dd36	fffffffffff84154	;
        ///Divide;586c42a9b165dd36	fffffffffff84154	;00000000004c4b40	0000000000000000	;e60342fc92962540	ffffffffffffffff	;
        ///Multiply;e60342fc92962540	ffffffffffffffff	;00000000004c4b40	0000000000000000	;586c42a9b1731000	fffffffffff84154	;
        ///Divide;586c42a9b1731000	fffffffffff84154	;000000000012a065	0000000000000000	;958e83d29739c5ff	ffffffffffffffff	;
        ///.
        /// </summary>
        internal static string mul_tc1_all_bin_op {
            get {
                return ResourceManager.GetString("mul_tc1_all_bin_op", resourceCulture);
            }
        }
    }
}

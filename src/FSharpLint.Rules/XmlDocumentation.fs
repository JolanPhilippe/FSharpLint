﻿(*
    FSharpLint, a linter for F#.
    Copyright (C) 2014 Matthew Mcveigh

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

namespace FSharpLint.Rules

/// Rules to enforce the use of XML documentation in various places.
module XmlDocumentation =

    open System
    open Microsoft.FSharp.Compiler.Ast
    open Microsoft.FSharp.Compiler.Range
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open FSharpLint.Framework.Ast
    open FSharpLint.Framework.Configuration
    open FSharpLint.Framework.LoadVisitors

    [<Literal>]
    let AnalyserName = "XmlDocumentation"

    let configExceptionHeader config name =
        match isRuleEnabled config AnalyserName name with
            | Some(_, ruleSettings) when ruleSettings.ContainsKey "Enabled" ->
                match ruleSettings.["Enabled"] with
                    | Enabled(true) -> true
                    | _ -> false
            | Some(_)
            | None -> false

    let isPreXmlDocEmpty (preXmlDoc:PreXmlDoc) =
        match preXmlDoc.ToXmlDoc() with
            | XmlDoc(xs) ->
                let atLeastOneThatHasText ars = ars |> Array.exists (fun s -> not (String.IsNullOrWhiteSpace(s))) |> not
                xs |> atLeastOneThatHasText
            | _ -> false

    let visitor visitorInfo checkFile astNode =
        match astNode.Node with
            | AstNode.ExceptionRepresentation(SynExceptionRepr.ExceptionDefnRepr(_, unionCase, _, xmlDoc, _, range)) ->
                if configExceptionHeader visitorInfo.Config "ExceptionDefinitionHeader" &&
                    astNode.IsSuppressed(AnalyserName, "ExceptionDefinitionHeader") |> not then
                    if isPreXmlDocEmpty xmlDoc then
                        visitorInfo.PostError range (FSharpLint.Framework.Resources.GetString("RulesXmlDocumentationExceptionError"))
            | AstNode.TypeDefinition(SynTypeDefn.TypeDefn(coreInfo, typeDefnRep, memberDefn, rng)) ->
                if configExceptionHeader visitorInfo.Config "TypeDefinitionHeader" &&
                    astNode.IsSuppressed(AnalyserName, "TypeDefinitionHeader") |> not then
                    let (SynComponentInfo.ComponentInfo(_, _, _, _, xmlDoc, _, _, range)) = coreInfo
                    if isPreXmlDocEmpty xmlDoc then
                        visitorInfo.PostError range (FSharpLint.Framework.Resources.GetString("RulesXmlDocumentationTypeError"))
            | _ -> ()

        Continue

    type RegisterXmlDocumentationVisitor() =
        let plugin =
            {
                Name = AnalyserName
                Visitor = Ast(visitor)
            }

        interface IRegisterPlugin with
            member this.RegisterPlugin with get() = plugin
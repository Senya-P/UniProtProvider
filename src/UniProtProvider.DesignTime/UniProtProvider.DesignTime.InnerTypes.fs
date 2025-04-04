namespace UniProtProvider.DesignTime
open ProviderImplementation.ProvidedTypes
open UniProtProvider.RunTime
open Client.UniProtClient
open Types

// --------------------------------------------------------------------------------------
// Nested types used for type provider generation
// --------------------------------------------------------------------------------------
module internal InnerTypes =
    let mutable private count = 0
    /// serves to generate unique type names
    let private nextNumber() = count <- count + 1; count 

    let getProteinProperties (props : array<UniProtKBIncomplete>) () =
        props
        |> Array.map (fun i ->
                // recommended name is not always present
                let name = 
                    if i.proteinDescription.recommendedName.IsSome then
                        i.proteinDescription.recommendedName.Value.fullName.value
                    else if i.proteinDescription.submissionNames.IsSome then
                        i.proteinDescription.submissionNames.Value[0].fullName.value
                    else if i.proteinDescription.alternativeNames.IsSome then
                        i.proteinDescription.alternativeNames.Value[0].fullName.value
                    else
                        ""
                let propertyName = name + " (" + i.uniProtkbId + ")"
                let value = i.uniProtkbId
                ProvidedProperty(propertyName = propertyName,
                    propertyType = typeof<Protein>,
                    getterCode = (fun _ -> <@@ getProteinById value |> Async.RunSynchronously @@>))
        )
        |> Array.toList

    let getOrganismProperties (props: array<TaxonomyIncomplete>) () =
        props
        |> Array.map (fun i ->
                let name = System.String.Concat(i.scientificName, " (", i.taxonId, ")")
                let value = i.taxonId
                ProvidedProperty(
                    propertyName=name,
                    propertyType = typeof<Taxonomy>,
                    getterCode = (fun _ -> <@@ getOrganismById value |> Async.RunSynchronously @@>)
                )
        )
        |> Array.toList

    /// Recursively generates the next set of results
    let rec getNext (param: Params) (outerType: ProvidedTypeDefinition) () =
        let next = ProvidedTypeDefinition(
            "InnerType" + string(nextNumber()),
            Some typeof<obj>,
            hideObjectMethods=true
        )
        next.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))

        match param.entity with
        | Entity.Protein -> 
            let result = getProteinsByKeyWord param |> Async.RunSynchronously
            next.AddMembersDelayed (getProteinProperties result.results)
        | Entity.Taxonomy ->
            let result = getOrganismsByKeyWord param |> Async.RunSynchronously
            next.AddMembersDelayed (getOrganismProperties result.results)

        outerType.AddMember next

        let cursor = getCursor param |> Async.RunSynchronously
        if cursor.IsSome then
            let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
            next.AddMemberDelayed (getNext nextParam next)

        ProvidedProperty(
            propertyName="More...",
            propertyType = next,
            getterCode = (fun _ -> <@@ obj() @@>)
        )
    /// Generates a method to retrieve proteins by organism name
    let getByOrganism (param : Params) (outerType: ProvidedTypeDefinition) () = 
        let byOrganism = ProvidedMethod("ByOrganism", [], typeof<obj>)
        byOrganism.DefineStaticParameters(
            [ProvidedStaticParameter("Name", typeof<string>)], 
            fun methName args ->
            let name = args.[0] :?> string
            param.organism <- name

            let t = ProvidedTypeDefinition(
                "InnerType" + string(nextNumber()),
                Some typeof<obj>,
                true)
            let result = getProteinsByKeyWord param |> Async.RunSynchronously
            t.AddMembersDelayed(getProteinProperties result.results)

            let cursor = getCursor param |> Async.RunSynchronously
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                t.AddMemberDelayed(getNext nextParam t)
            outerType.AddMember(t)

            let m = ProvidedMethod(
                methName, 
                [], 
                t, 
                invokeCode = fun _ -> <@@ obj() @@>
            )
            outerType.AddMember(m)
            m
        )
        byOrganism
    /// Generates a method to find proteins related to the taxonomy entry
    let getFindRelated (param : Params) (outerType: ProvidedTypeDefinition) () =
        let t = ProvidedTypeDefinition(
            "InnerType" + string(nextNumber()),
            Some typeof<obj>
        )
        let result = getProteinsByKeyWord param |> Async.RunSynchronously
        t.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
        t.AddMembersDelayed(getProteinProperties result.results)

        let cursor = getCursor param |> Async.RunSynchronously
        if cursor.IsSome then
            let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
            t.AddMemberDelayed(getNext nextParam t)
        outerType.AddMember(t)

        ProvidedMethod(
            methodName="FindRelated",
            parameters = [],
            returnType = t,
            invokeCode = (fun _ -> <@@ obj() @@>)
        )

    let getOrganismResults (props: array<TaxonomyIncomplete>) (outerType: ProvidedTypeDefinition) () =
        props
        |> Array.map (fun i ->
                let organismResult =  ProvidedTypeDefinition(
                    "InnerType" + string(nextNumber()),
                    Some typeof<obj>,
                    hideObjectMethods=true)
                organismResult.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
                outerType.AddMember organismResult

                let name = System.String.Concat(i.scientificName, " (", i.taxonId, ")")
                let value = i.taxonId
                let p = ProvidedProperty(
                    propertyName=name,
                    propertyType = typeof<Taxonomy>,
                    getterCode = (fun _ -> <@@ getOrganismById value |> Async.RunSynchronously @@>)
                )
                organismResult.AddMemberDelayed (fun _-> p)

                let param = Params("")
                param.taxonId <- string(i.taxonId)
                organismResult.AddMemberDelayed(getFindRelated param outerType)

                ProvidedProperty(
                    propertyName=name,
                    propertyType = organismResult,
                    getterCode = (fun _ -> <@@ obj() @@>)
                )
        )
        |> Array.toList


    let getSuggestions (sug : array<Suggestion>) (outerType: ProvidedTypeDefinition) () =
        sug
        |> Array.map (fun i ->
            let keyword = i.query.Value.Split [|' '|]
            let name = keyword[0]
            let param = Params(name)
            let result = getProteinsByKeyWord param |> Async.RunSynchronously
            let suggested =  ProvidedTypeDefinition(
                "InnerType" + string(nextNumber()),
                Some typeof<obj>,
                hideObjectMethods=true)

            suggested.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
            suggested.AddMembersDelayed(getProteinProperties result.results)
            suggested.AddMemberDelayed(getByOrganism param suggested)

            let cursor = getCursor param |> Async.RunSynchronously
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                suggested.AddMemberDelayed(getNext nextParam suggested)

            outerType.AddMember suggested
            ProvidedProperty(
                propertyName = name,
                propertyType = suggested,
                getterCode = (fun _ -> <@@ obj() @@>)
            )
        )
        |> Array.toList

namespace UniProtProvider.DesignTime
open ProviderImplementation.ProvidedTypes
open UniProtProvider.RunTime
open Client.UniProtClient
open Types

module internal InnerTypes =
    let mutable count = 0
    let nextNumber() = count <- count + 1; count // serves to generate unique type names

    let getProteinProperties (props : array<UniProtKBIncomplete>) () =
        [
            for i in props do
                let name = System.String.Concat(i.proteinDescription.recommendedName.Value.fullName.value, " (", i.uniProtkbId, ")")
                let value = i.uniProtkbId
                let p =
                    ProvidedProperty(propertyName = name,
                    propertyType = typeof<Protein>,
                    getterCode = (fun _ -> <@@ getProteinById value @@>))
                p
        ]

    let getOrganismProperties (props: array<TaxonomyIncomplete>) () =
        [
            for i in props do
                let name = System.String.Concat(i.scientificName, " (", i.taxonId, ")")
                let value = i.taxonId
                let p =
                    ProvidedProperty(propertyName=name,
                    propertyType = typeof<Taxonomy>,
                    getterCode = (fun _ -> <@@ getOrganismById value @@>))
                p
        ]

    let rec getByOrganism (param : Params) (outerType: ProvidedTypeDefinition) () = 
        let byOrganism = ProvidedMethod("ByOrganism", [], typeof<obj>)
        byOrganism.DefineStaticParameters([ProvidedStaticParameter("Name", typeof<string>)], fun methName args ->
            let name = args.[0] :?> string
            param.organism <- name

            let t = ProvidedTypeDefinition("InnerType" + string(nextNumber()), Some typeof<obj>, true)
            let result = getProteinsByKeyWord param
            t.AddMembersDelayed(getProteinProperties result.results)
            t.AddMemberDelayed(getByOrganism param t)

            let cursor = getCursor param
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                t.AddMemberDelayed(getNext nextParam t)
            outerType.AddMember(t)

            let m = ProvidedMethod(methName, [], t, invokeCode = fun _ -> <@@ obj() @@>)
            outerType.AddMember(m)
            m
        )
        byOrganism

    and getNext (param: Params) (outerType: ProvidedTypeDefinition) () =
        let next = 
            ProvidedTypeDefinition("InnerType" + string(nextNumber()),
            Some typeof<obj>,
            hideObjectMethods=true)
        next.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))

        match param.entity with
        | Entity.Protein -> 
            let result = getProteinsByKeyWord param
            next.AddMembersDelayed(getProteinProperties result.results)
            next.AddMemberDelayed (getByOrganism param next)
        | Entity.Taxonomy ->
            let result = getOrganismsByKeyWord param
            next.AddMembersDelayed (getOrganismProperties result.results)
        | _ -> ()

        outerType.AddMember next

        let cursor = getCursor param
        if cursor.IsSome then
            let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
            next.AddMemberDelayed(getNext nextParam next)

        let p =
            ProvidedProperty(propertyName="More...",
            propertyType = next,
            getterCode = (fun _ -> <@@ obj() @@>))
        p

    let getFindRelated (param : Params) (outerType: ProvidedTypeDefinition) =
        let t = ProvidedTypeDefinition("InnerType" + string(nextNumber()), Some typeof<obj>)
        let result = getProteinsByKeyWord param
        t.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
        t.AddMembersDelayed(getProteinProperties result.results)

        let cursor = getCursor param
        if cursor.IsSome then
            let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
            t.AddMemberDelayed(getNext nextParam t)
        outerType.AddMember(t)

        let p =
            ProvidedMethod(methodName="FindRelated",
            parameters = [],
            returnType = t,
            invokeCode = (fun _ -> <@@ obj() @@>))
        p

    let getOrganismResults (props: array<TaxonomyIncomplete>) (outerType: ProvidedTypeDefinition) () =
        [
            for i in props do
                let organismResult =  ProvidedTypeDefinition("InnerType" + string(nextNumber()),
                    Some typeof<obj>,
                    hideObjectMethods=true)
                organismResult.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
                outerType.AddMember organismResult

                let name = System.String.Concat(i.scientificName, " (", i.taxonId, ")")
                let value = i.taxonId
                let p =
                    ProvidedProperty(propertyName=name,
                    propertyType = typeof<Taxonomy>,
                    getterCode = (fun _ -> <@@ getOrganismById value @@>))
                organismResult.AddMember p

                let param = Params("")
                param.taxonId <- string(i.taxonId)
                organismResult.AddMember(getFindRelated param outerType)

                let result = 
                    ProvidedProperty(propertyName=name,
                    propertyType = organismResult,
                    getterCode = (fun _ -> <@@ obj() @@>))
                result
        ]


    let getSuggestions (sug : array<Suggestion>) (outerType: ProvidedTypeDefinition) () =
        [
        for i in sug do
            let keyword = i.query.Value
            let param = Params(keyword)
            let result = getProteinsByKeyWord param
            let suggested = 
                ProvidedTypeDefinition("InnerType" + string(nextNumber()),
                Some typeof<obj>,
                hideObjectMethods=true)

            suggested.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
            suggested.AddMembersDelayed(getProteinProperties result.results)

            let param = Params(keyword)
            suggested.AddMemberDelayed(getByOrganism param suggested)

            let cursor = getCursor param
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                suggested.AddMemberDelayed(getNext nextParam suggested)

            outerType.AddMember suggested
            let p =
                ProvidedProperty(propertyName = keyword,
                propertyType = suggested,
                getterCode = (fun _ -> <@@ obj() @@>))
            p
        ]
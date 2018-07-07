module Hyperbolic.Main

open System

open Hyperbolic.Draw
open Hyperbolic.Transformations

[<EntryPoint>]
let main _ =
    let fP = 6.0
    let startScale = 0.05
    let endScale = 0.4
    let rotatePi idx frac scale = Point(scale * Math.Cos(Math.PI*idx/frac), scale * Math.Sin(Math.PI*idx/frac))
    let rotPiP idx = rotatePi idx fP
    let rotPi2P idx = rotatePi idx (2.0 * fP)

    let polygonInstuctions = 
        seq {1.0 .. (2.0 * fP)}
        |> Seq.toList
        |> List.map (fun idx -> DrawLine(rotPiP idx endScale, rotPiP (idx + 1.0) endScale))
    
    let lineInstructions = 
        seq {1.0 .. 2.0 .. (4.0 * fP - 1.0)}
        |> Seq.toList
        |> List.map (fun idx -> DrawLine(rotPi2P idx startScale, rotPi2P idx endScale))

    let polygonPattern = { P = 6; Q = 4; Layers = 4; InitialPattern = polygonInstuctions }
    let polygonFileProps = { ImageSize = 700; BoundedCircleRadius = 300.0; FileName = "polygons.png"; DrawTesselation = false }
    let linePattern = { polygonPattern with InitialPattern = lineInstructions }
    let lineFileProps = { polygonFileProps with FileName = "lines.png" }
    drawHyperbolicPattern polygonPattern polygonFileProps
    drawHyperbolicPattern linePattern lineFileProps

    let transformationMatrices = transformationMatrices polygonPattern.P polygonPattern.Q
    let firstTriangleLine = DrawLine(rotPi2P 1.0 startScale, rotPi2P 1.0 endScale)
    let reflectEdgeBisectorInstructions = [firstTriangleLine; transformAction transformationMatrices.ReflectEdgeBisector firstTriangleLine]
    let reflectHypotenuseInstructions = [firstTriangleLine; transformAction transformationMatrices.ReflectHypotenuse firstTriangleLine]
    let reflectPgonEdgeInstructions = [firstTriangleLine; transformAction transformationMatrices.ReflectPgonEdge firstTriangleLine]
    let rotatePinstructions =  [firstTriangleLine; transformAction transformationMatrices.RotateP firstTriangleLine]
    let rotateQInstructions =  [firstTriangleLine; transformAction transformationMatrices.RotateQ firstTriangleLine]
    
    let reflectEdgeBisectorPattern = { polygonPattern with InitialPattern = reflectEdgeBisectorInstructions; Layers = 1 }
    let reflectEdgeBisectorFileProps = { polygonFileProps with DrawTesselation = true; FileName = "edgebisector.png" }
    drawHyperbolicPattern reflectEdgeBisectorPattern reflectEdgeBisectorFileProps
    drawHyperbolicPattern { reflectEdgeBisectorPattern with InitialPattern = reflectHypotenuseInstructions } {reflectEdgeBisectorFileProps with FileName = "hypotenuse.png" }
    drawHyperbolicPattern { reflectEdgeBisectorPattern with InitialPattern = reflectPgonEdgeInstructions } {reflectEdgeBisectorFileProps with FileName = "pgonedge.png" }
    drawHyperbolicPattern { reflectEdgeBisectorPattern with InitialPattern = rotatePinstructions } {reflectEdgeBisectorFileProps with FileName = "rotateP.png" }
    drawHyperbolicPattern { reflectEdgeBisectorPattern with InitialPattern = rotateQInstructions } {reflectEdgeBisectorFileProps with FileName = "rotateQ.png" }
    
    0
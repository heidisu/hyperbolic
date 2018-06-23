module Hyperbolic.Main

open System

open Hyperbolic.Draw

[<EntryPoint>]
let main _ =
    let c = 0.4
    let d = 0.05
    let rotatePi idx frac c = Point(c * Math.Cos(Math.PI*idx/frac), c * Math.Sin(Math.PI*idx/frac))
    let rotPiP idx = rotatePi idx fP
    let rotPi2P idx = rotatePi idx (2.0 * fP)

    let polygonInstuctions = 
        seq {1.0 .. (2.0 * fP)}
        |> Seq.toList
        |> List.map (fun idx -> DrawLine(rotPiP idx c, rotPiP (idx + 1.0) c))
    
    let lineInstructions = 
        seq {1.0 .. 2.0 .. (4.0 * fP - 1.0)}
        |> Seq.toList
        |> List.map (fun idx -> DrawLine(rotPi2P idx d, rotPi2P idx c))

    drawImage "polygons.png" 4 polygonInstuctions
    drawImage "lines.png" 4 lineInstructions

    drawImageWithTesselation "edgebisector.png" reflectEdgeBisector [DrawLine(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawImageWithTesselation "hypotenuse.png" reflectHypotenuse [DrawLine(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawImageWithTesselation "pgonedge.png" reflectPgonEdge [DrawLine(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawImageWithTesselation "rotP.png" rotateP [DrawLine(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    drawImageWithTesselation "rotQ.png" rotateQ [DrawLine(rotPi2P 1.0 d, rotPi2P 1.0 c)]
    
    0
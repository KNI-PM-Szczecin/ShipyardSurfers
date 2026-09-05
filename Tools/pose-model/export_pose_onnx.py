import argparse
import os
import shutil
from pathlib import Path

import onnx
from ultralytics import YOLO

SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_OUTPUT_DIR = SCRIPT_DIR.parents[1] / "Assets" / "Resources" / "MotionControl"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Export an Ultralytics YOLO pose model to ONNX for Unity Inference Engine.")
    parser.add_argument("--model", default="yolo26n-pose.pt", help="Ultralytics weights name or path (downloaded automatically).")
    parser.add_argument("--imgsz", type=int, default=320, help="Square input size of the exported model.")
    parser.add_argument("--opset", type=int, default=17)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--output-name", default=None, help="File name without extension; defaults to <model>-<imgsz>.")
    return parser.parse_args()


def export(model_name: str, imgsz: int, opset: int) -> Path:
    model = YOLO(model_name)
    common = dict(format="onnx", imgsz=imgsz, opset=opset, simplify=True, dynamic=False, half=False)
    try:
        exported = model.export(end2end=False, **common)
    except (TypeError, SyntaxError):
        exported = model.export(**common)
    return Path(exported)


def strip_attributes_ignored_by_inference_engine(path: Path) -> None:
    model = onnx.load(str(path))
    for node in model.graph.node:
        if node.op_type == "MaxPool":
            keep = [a for a in node.attribute if not (a.name == "ceil_mode" and a.i == 0)]
        elif node.op_type == "Resize":
            mode = next((a.s.decode() for a in node.attribute if a.name == "mode"), "nearest")
            keep = [a for a in node.attribute if a.name != "cubic_coeff_a" or mode == "cubic"]
        else:
            continue
        del node.attribute[:]
        node.attribute.extend(keep)
    onnx.save(model, str(path))


def main() -> None:
    args = parse_args()
    os.chdir(SCRIPT_DIR)
    exported = export(args.model, args.imgsz, args.opset)
    strip_attributes_ignored_by_inference_engine(exported)

    stem = args.output_name or f"{Path(args.model).stem}-{args.imgsz}"
    args.output_dir.mkdir(parents=True, exist_ok=True)
    target = args.output_dir / f"{stem}.onnx"
    shutil.copyfile(exported, target)
    print(f"Exported {exported} -> {target}")


if __name__ == "__main__":
    main()

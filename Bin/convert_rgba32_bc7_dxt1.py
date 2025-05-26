import ctypes
import os
import re
import struct

from PIL import Image

NAME_PATTERN = re.compile(r"^(.+?)-(CAB-[0-9a-f]{32}|BuildPlayer-.+\.sharedAssets)-(-?\d+)")
INPUT_FOLDERS = [
  "input",
  "input-bc7",
  "input-dxt1",
]


dll = ctypes.CDLL("Bin/TexToolWrap.dll")
dll.EncodeByISPC.argtypes = [
  ctypes.c_void_p,  # data
  ctypes.c_void_p,  # outBuf
  ctypes.c_int,  # mode
  ctypes.c_int,  # level
  ctypes.c_uint,  # width
  ctypes.c_uint,  # height
]
dll.EncodeByISPC.restype = ctypes.c_uint


def encode_data(data: bytes, mode: int, level: int, width: int, height: int) -> bytes:
  in_buf = ctypes.create_string_buffer(data)
  in_ptr = ctypes.addressof(in_buf)

  out_size = len(data) * 4
  out_buf = ctypes.create_string_buffer(out_size)
  out_ptr = ctypes.addressof(out_buf)

  result_len = dll.EncodeByISPC(
    in_ptr, out_ptr, ctypes.c_int(mode), ctypes.c_int(level), ctypes.c_uint(width), ctypes.c_uint(height)
  )

  return bytes(out_buf[:result_len])


for folder in os.listdir("Images"):
  if "CAB-" not in folder and ".sharedAssets" not in folder:
    continue
  for input_folder in INPUT_FOLDERS:
    if not os.path.exists(f"Images/{folder}/{input_folder}"):
      continue
    for file_name in os.listdir(f"Images/{folder}/{input_folder}"):
      if not file_name.endswith(".png"):
        continue

      if not NAME_PATTERN.match(file_name):
        continue

      image_name, cab_id, index = NAME_PATTERN.match(file_name).groups()
      index = int(index)
      index_hex = struct.pack(">q", index).hex().zfill(16)

      image = Image.open(f"Images/{folder}/{input_folder}/{file_name}")
      image = image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
      image_bytes = image.tobytes("raw")
      if input_folder == "input-bc7":
        image_bytes = encode_data(image_bytes, 25, 5, image.width, image.height)
      elif input_folder == "input-dxt1":
        image_bytes = encode_data(image_bytes, 10, 5, image.width, image.height)

      output_path = f"Patch/{cab_id}/Texture2D/{index_hex}.res"
      os.makedirs(os.path.dirname(output_path), exist_ok=True)
      with open(output_path, "wb") as output_file:
        output_file.write(image_bytes)
      print(f"Processed {cab_id}/{image_name} -> {output_path}")

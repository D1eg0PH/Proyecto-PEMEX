import os
import sys
import tkinter as tk
from tkinter import ttk, messagebox, filedialog
from tkcalendar import DateEntry
from datetime import datetime
from PIL import Image, ImageTk
import fitz  # PyMuPDF
from io import BytesIO
import re
import subprocess
import json
import threading


def get_base_dir():
    if getattr(sys, 'frozen', False):
        return os.path.dirname(sys.executable)
    else:
        return os.path.dirname(os.path.abspath(__file__))


BASE_DIR = get_base_dir()
CONFIG_FILE = os.path.join(BASE_DIR, "config.json")

CATALOGO_DESCRIPCIONES = [
    "IP VALVULA DE ALIVIO TERMICO PSV",
    "DESCARGADERA PATIN",
    "MOTOBOMBA COMBUSTION CI",
    "CARCAMO ACEITOSO",
    "RIEGO",
    "LIMPIEZA DE FILTROS CANASTA/SUCCION"
    "BOMBA ENGRANES",
    "AGUA SIPA",
    "TRANSFORMADOR SECO",
    "DESCARGADERA PATIN",
    "PD1CAOP LLENADERA",
    "EDIFICACION TORRE",
    "LIMPIEZA DE FILTROS",
    "SUBESTACION COMPACTA",
    "YPS SIMCOT",
    "TRANSFORMADOR POTENCIA",
    "BANCO CAPACITORES",
    "MXEX 13092VOL",
    "VALVULAS DE RECIRCULACION",
    "COBERTIZO BOMBAS PROCESO",
    "EDIFICACION",
    "PRUEBAS ALTO NIVEL",
    "CAJA DE CONEXIONES",
    "FREIGHTLINER",
    "PRUEBA DE ALARMAS SECTORIALES",
    "COBERTIZO",
    "MOTOBOMBA ELECTRICA CI",
    "CORRIDAS DE VERIFICACION DESCARGADERA",
    "CALIBRACION DE MDP",
    "CALIBRACION DE MEDIDOR",
    "PV1MT BOMBA ENGRANES *PATIN*"
]

CATALOGO_OPERADORES = [
    "INS01",
    "EIN01",
    "CMEE01AM",
    "OPAUX01",
    "IAMC01CS",
    "JMT01",
    "BCC01",
    "PSERV01",
    "AYTPAT01",
    "JOP01",
    "CMEEC01AM",
    "CMEC01AM",
]


class PDFOrganizerApp:
    def cargar_config(self):
        config = {}
        if os.path.exists(CONFIG_FILE):
            try:
                with open(CONFIG_FILE, "r", encoding="utf-8") as f:
                    config = json.load(f)
            except Exception:
                config = {}
        return config

    def ver_observaciones(self):
        selection = self.tree.selection() # <--- CAMBIO
        if not selection:
            messagebox.showinfo("Observaciones", "No hay orden seleccionada.")
            return

        item_id = selection[0]            # <--- CAMBIO
        index = self.tree.index(item_id)  # <--- CAMBIO
        pdf_data = self.filtered_list[index]
        
        texto = pdf_data.get('observaciones', '').strip()

        if not texto:
            messagebox.showinfo("Observaciones", "Esta orden no tiene observaciones en rojo.")
            return

        # Ventana nueva
        win = tk.Toplevel(self.root)
        win.title(f"Observaciones - {pdf_data['filename']}")
        win.geometry("600x400")

        txt = tk.Text(win, wrap='word')
        txt.pack(fill='both', expand=True)
        txt.insert('1.0', texto)
        txt.config(state='disabled')


    def guardar_config(self):
        try:
            with open(CONFIG_FILE, "w", encoding="utf-8") as f:
                json.dump(self.config, f, indent=2)
        except Exception as e:
            messagebox.showerror("Error", f"No se pudo guardar la configuración:\n{e}")

    def __init__(self, root):
        self.root = root
        self.root.title("Organizador de PDFs - Órdenes de Trabajo")
        self.root.geometry("1000x750")
        style = ttk.Style()
        style.theme_use("clam")
        

        BG = "#f2f2f2"
        PANEL_BG = "#ffffff"
        FG = "#1f2933"

        self.root.configure(bg=BG)
        style.configure("TFrame", background=BG)
        style.configure("Panel.TFrame", background=PANEL_BG)
        style.configure("TLabel", background=PANEL_BG, foreground=FG,
                        font=("Segoe UI", 9))
        style.configure("TButton", padding=4, font=("Segoe UI", 9))


        # Configuración de carpeta: se pregunta solo la primera vez
        self.config = self.cargar_config()

        self.carpeta_pdfs = r"C:\Users\diego\Documents\PDFs"
        #self.carpeta_pdfs = self.config.get("carpeta_pdfs")
        if not self.carpeta_pdfs or not os.path.isdir(self.carpeta_pdfs):
            self.carpeta_pdfs = filedialog.askdirectory(
                title="Selecciona la carpeta donde están los PDFs"
            )
            if not self.carpeta_pdfs:
                messagebox.showerror("Error", "No se seleccionó carpeta de PDFs.")
                root.destroy()
                return
            self.config["carpeta_pdfs"] = self.carpeta_pdfs
            self.guardar_config()

        self.pdf_list = []
        self.filtered_list = []
        self.preview_image = None
        self.use_date_filter = tk.BooleanVar(value=False)
        self.sort_order = tk.StringVar(value="nombre_asc")
        self.descripcion_var = tk.StringVar(value="-- Todos --")
        self.operador_var = tk.StringVar(value="-- Todos --")
        self.operadores_catalogo = CATALOGO_OPERADORES.copy()
        self.create_widgets()
        self.load_pdfs()

    def create_widgets(self):
        main_frame = ttk.Frame(self.root, style="TFrame")
        main_frame.pack(fill='both', expand=True, padx=10, pady=10)

        left_frame = ttk.Frame(main_frame, style="TFrame")
        left_frame.pack(side='left', fill='both', expand=True, padx=(0, 5))

        right_frame = ttk.Frame(main_frame, style="TFrame")
        right_frame.pack(side='right', fill='both', expand=True, padx=(5, 0))
       

        # === Filtros ===
        filter_frame = ttk.LabelFrame(left_frame, text="Filtros de búsqueda", padding=8)
        filter_frame.pack(fill='x', pady=(0, 5))

        # Buscador por texto en descripción
        ttk.Label(filter_frame, text="Buscar en descripción:").grid(row=0, column=0, sticky='w', pady=2)
        self.search_var = tk.StringVar()
        self.search_entry = ttk.Entry(filter_frame, textvariable=self.search_var, width=40)
        self.search_entry.grid(row=0, column=1, sticky='ew', pady=2, padx=5)
        self.search_entry.bind("<KeyRelease>", lambda e: self.apply_filters())

        ttk.Label(filter_frame, text="Filtrar por descripción:").grid(row=1, column=0, sticky='w', pady=2)
        self.desc_combo = ttk.Combobox(filter_frame, textvariable=self.descripcion_var,
                                       state='readonly', width=40)
        self.desc_combo.grid(row=1, column=1, sticky='ew', pady=2, padx=5)
        self.desc_combo['values'] = ["-- Todos --"] + CATALOGO_DESCRIPCIONES
        self.desc_combo.bind("<<ComboboxSelected>>", lambda e: self.apply_filters())

        ttk.Label(filter_frame, text="Filtrar por operador:").grid(row=2, column=0, sticky='w', pady=2)
        self.operador_combo = ttk.Combobox(filter_frame, textvariable=self.operador_var,
                                           state='readonly', width=40)
        self.operador_combo.grid(row=2, column=1, sticky='ew', pady=2, padx=5)
        self.operador_combo['values'] = ["-- Todos --"] + CATALOGO_OPERADORES
        self.operador_combo.bind("<<ComboboxSelected>>", lambda e: self.apply_filters())

        date_frame = ttk.Frame(filter_frame, style="Panel.TFrame")
        date_frame.grid(row=3, column=0, columnspan=2, sticky='w', pady=3)
        self.date_check = ttk.Checkbutton(date_frame, text="Filtrar por fecha de inicio:",
                                          variable=self.use_date_filter,
                                          command=self.apply_filters)
        self.date_check.pack(side='left')
        self.date_picker = DateEntry(date_frame, width=12, background='darkblue',foreground='white', borderwidth=2,date_pattern='dd.mm.yyyy')
        self.date_picker.bind("<<DateEntrySelected>>", lambda e: self.apply_filters())
        self.date_picker.pack(side='left', padx=10)

        sort_frame = ttk.Frame(filter_frame, style="Panel.TFrame")
        sort_frame.grid(row=4, column=0, columnspan=2, sticky='w', pady=3)
        ttk.Label(sort_frame, text="Ordenar por:").pack(side='left')
        self.sort_combo = ttk.Combobox(sort_frame, textvariable=self.sort_order,values=["Nombre (A-Z)", "Nombre (Z-A)","Fecha (Más reciente)", "Fecha (Más antigua)"], state='readonly', width=22)
        self.sort_combo.current(0)
        self.sort_combo.bind("<<ComboboxSelected>>", lambda e: self.apply_filters())
        self.sort_combo.pack(side='left', padx=8)

        filter_frame.columnconfigure(1, weight=1)

        # === Botones ===
        button_frame = ttk.Frame(left_frame, style="TFrame")
        button_frame.pack(fill='x', pady=5)
        ttk.Button(button_frame, text="Recargar PDFs", command=self.load_pdfs).pack(side='left', padx=5)
        ttk.Button(button_frame, text="Limpiar filtros", command=self.clear_filters).pack(side='left', padx=5)
        self.edit_button = ttk.Button(button_frame, text="Editar PDF", command=self.edit_pdf, state='disabled')
        self.edit_button.pack(side='left', padx=5)
        self.obs_button = ttk.Button(button_frame, text="Ver Observaciones",command=self.ver_observaciones, state='disabled')
        self.obs_button.pack(side='left', padx=5, expand=True, fill='x')


        # === Lista de órdenes ===
        list_frame = ttk.LabelFrame(left_frame, text="Órdenes de trabajo encontradas", padding=4)
        list_frame.pack(fill='both', expand=True)
        
        self.tree = ttk.Treeview(list_frame, columns=("Desc", "Op", "Fecha", "Estado", "Archivo"), show="headings")
        self.tree.heading("Desc", text="Descripción")
        self.tree.heading("Op", text="Operador")
        self.tree.heading("Fecha", text="Fecha")
        self.tree.heading("Estado", text="Estado")
        self.tree.heading("Archivo", text="Archivo")

        # ... configurar columnas ...
        self.tree.pack(fill='both', expand=True)

        #Conectamos el clic simple a la vista previa
        self.tree.bind("<<TreeviewSelect>>", self.preview_pdf)

        # Conectamos el doble clic para abrir el archivo
        self.tree.bind("<Double-1>", self.open_pdf)

        
        self.result_label = ttk.Label(left_frame, text="", font=('Segoe UI', 9, 'bold'),background="#f2f2f2", foreground="#111")
        self.result_label.pack(anchor='w', pady=(3, 0))

        # === Vista previa a la derecha ===
        preview_frame = ttk.LabelFrame(right_frame, text="Vista previa (doble clic para abrir)", padding=4)
        preview_frame.pack(fill='both', expand=True)

        self.canvas = tk.Canvas(preview_frame, width=450, height=550,
                                bg="#202020", highlightthickness=0)
        self.canvas.pack(fill='both', expand=True)






    def match_descripcion_from_catalog(self, full_text):
        text_upper = full_text.upper()
        for descripcion in CATALOGO_DESCRIPCIONES:
            palabras_clave = descripcion.upper().split()
            coincide = all(palabra in text_upper for palabra in palabras_clave)
            if coincide:
                return descripcion
        return "Sin categoría"

    def is_pdf_closed(self, pdf_path):
        """Detecta si el PDF tiene el texto 'CERRADO PM SAP'"""
        try:
            doc = fitz.open(pdf_path)
            for page in doc:
                text = page.get_text()
                if "CERRADO PM SAP" in text.upper():
                    doc.close()
                    return True
            doc.close()
            return False
        except Exception as e:
            print(f"Error al verificar cierre del PDF {pdf_path}: {e}")
            return False

    def extract_pdf_metadata(self, pdf_path):
            try:
                doc = fitz.open(pdf_path)
                full_text = ""
                observaciones = []
                cerrado = False  # Variable para detectar si está cerrado

                for page in doc:
                    full_text += page.get_text()
                    
                    # 1. DETECTAR SI ESTÁ CERRADO (Mientras leemos)
                    if "CERRADO PM SAP" in page.get_text().upper():
                        cerrado = True

                    # 2. DETECTAR COLORES (Observaciones)
                    text_dict = page.get_text("dict")
                    for block in text_dict["blocks"]:
                        for line in block.get("lines", []):
                            for span in line.get("spans", []):
                                
                                # --- AQUÍ ESTÁ EL CAMBIO CLAVE ---
                                # Antes: if span.get("color") == 16711680: (Solo Rojo)
                                # Ahora: > 5 significa "Cualquier color que no sea negro"
                                color = span.get("color", 0)
                                if color > 5: 
                                    txt = span.get("text", "").strip()
                                    # Filtramos basura (números de página, encabezados vacíos, etc.)
                                    if txt and len(txt) > 2: 
                                        observaciones.append(txt)
                                # ---------------------------------

                doc.close() 

                metadata = {
                    'descripcion': '',
                    'fecha_inicio': None,
                    'operadores': [],
                    'cerrado': cerrado, # Usamos el que detectamos arriba
                    'raw_text': full_text,
                    'observaciones': " | ".join(observaciones).strip() if observaciones else ""
                }

                # ... (El resto de tu lógica de descripción y fecha sigue igual) ...
                
                # Lógica de Descripción
                descripcion_catalogo = self.match_descripcion_from_catalog(full_text)
                if descripcion_catalogo != "Sin categoría":
                    metadata['descripcion'] = descripcion_catalogo
                else:
                    desc_match = re.search(r"Descripción\s+(.+?)\s+Fecha de inicio", full_text, re.IGNORECASE)
                    if desc_match:
                        metadata['descripcion'] = desc_match.group(1).strip()
                    else:
                        desc_match = re.search(r"Descripción\s+(.+?)(?:\n|Puesto)", full_text, re.IGNORECASE)
                        metadata['descripcion'] = desc_match.group(1).strip() if desc_match else "Sin descripción"

                # Lógica de Fecha
                fecha_match = re.search(r"Fecha Ini m\.temp\s+(\d{2}\.\d{2}\.\d{4})", full_text)
                if not fecha_match:
                    fecha_match = re.search(r"Fecha de inicio\s+(\d{2}\.\d{2}\.\d{4})", full_text)
                if fecha_match:
                    try:
                        fecha_str = fecha_match.group(1)
                        metadata['fecha_inicio'] = datetime.strptime(fecha_str, "%d.%m.%Y")
                    except Exception:
                        pass

                # Lógica de Operadores
                operadores_encontrados = []
                texto_upper = full_text.upper()
                for op in CATALOGO_OPERADORES:
                    if op.upper() in texto_upper:
                        operadores_encontrados.append(op)
                metadata['operadores'] = operadores_encontrados

                return metadata

            except Exception as e:
                print(f"Error al extraer metadatos de {pdf_path}: {e}")
                return None


    def load_pdfs(self):
        # 1. Limpiar lista y mostrar mensaje de carga
        self.pdf_list = []
        self.result_label.config(text="Cargando PDFs... Por favor espere.")
        #self.apply_filters()
        self.filtered_list = self.pdf_list # Copia todo directo
        self.update_listbox() # Actualiza directo

        # 2. Deshabilitar botón para evitar doble clic
        # (Suponiendo que guardaste la referencia al botón recargar, si no, agrégala)

        # 3. Lanzar el hilo
        threading.Thread(target=self._load_pdfs_thread, daemon=True).start()

    def _load_pdfs_thread(self):
        # Esta función corre en paralelo
        temp_list = []
        try:
            if not os.path.isdir(self.carpeta_pdfs):
                return
        
            for filename in os.listdir(self.carpeta_pdfs):
                if filename.lower().endswith('.pdf'):
                    full_path = os.path.join(self.carpeta_pdfs, filename)
                    metadata = self.extract_pdf_metadata(full_path)
                    if metadata:
                        creation_time = datetime.fromtimestamp(os.path.getctime(full_path))
                        # Agregar a lista temporal
                        temp_list.append({
                            'filename': filename,
                            'creation_time': creation_time,
                            'descripcion': metadata['descripcion'],
                            'fecha_inicio': metadata['fecha_inicio'],
                            'operadores': metadata['operadores'],
                            'cerrado': metadata['cerrado'],
                            'observaciones': metadata['observaciones']            
                        })
        except Exception as e:
            messagebox.showerror("Error", f"Error al cargar PDFs: {e}")

        # 4. Volver al hilo principal para actualizar la UI
        self.root.after(0, self.finish_loading, temp_list)
        

    def finish_loading(self, loaded_data):
        self.pdf_list = loaded_data
        self.operador_combo['values'] = ["-- Todos --"] + CATALOGO_OPERADORES
        self.apply_filters()
        self.result_label.config(text=f"Carga completa. {len(self.pdf_list)} archivos.")
    

    def clear_filters(self):
        self.descripcion_var.set("-- Todos --")
        self.operador_var.set("-- Todos --")
        self.use_date_filter.set(False)
        self.apply_filters()

    def apply_filters(self):
        # 1. Obtenemos los valores de los filtros
        desc_filter = self.descripcion_var.get()
        operador_filter = self.operador_var.get()
        use_date = self.use_date_filter.get()
        
        # Validación de seguridad para la fecha
        try:
            date_filter = self.date_picker.get_date() if use_date else None
        except Exception:
            date_filter = None

        text_filter = self.search_var.get().strip().lower() if hasattr(self, "search_var") else ""

        # DEBUG: Ver qué filtros cree Python que tienes activos
        print(f"--- INICIO FILTRO ---")
        print(f"Filtros Activos -> Desc: '{desc_filter}' | Op: '{operador_filter}' | FechaActiva: {use_date} | Texto: '{text_filter}'")

        self.filtered_list = []
        
        for pdf_data in self.pdf_list:
            nombre_archivo = pdf_data['filename']
            
            # --- DIAGNÓSTICO DE CADA FILTRO ---

            # 1. Filtro Descripción
            if desc_filter != "-- Todos --":
                # Limpiamos espacios por si acaso
                desc_pdf = (pdf_data['descripcion'] or "").strip()
                if desc_pdf != desc_filter:
                    print(f"RECHAZADO '{nombre_archivo}': Descripción '{desc_pdf}' no es igual a '{desc_filter}'")
                    continue

            # 2. Filtro Operador
            if operador_filter != "-- Todos --":
                # Si la lista de operadores del PDF está vacía o no tiene al buscado
                if operador_filter not in pdf_data['operadores']:
                    print(f"RECHAZADO '{nombre_archivo}': Operador '{operador_filter}' no está en {pdf_data['operadores']}")
                    continue

            # 3. Filtro Fecha
            if use_date and date_filter:
                fecha_pdf = pdf_data['fecha_inicio']
                if not fecha_pdf: 
                    # Si activaste filtro de fecha, ¿qué hacemos con los que no tienen fecha?
                    # Por ahora los rechazamos para ser estrictos
                    print(f"RECHAZADO '{nombre_archivo}': No tiene fecha detectada")
                    continue
                    
                if fecha_pdf.date() != date_filter:
                    print(f"RECHAZADO '{nombre_archivo}': Fecha '{fecha_pdf.date()}' no coincide con '{date_filter}'")
                    continue

            # 4. Filtro Texto (Buscador)
            if text_filter:
                desc_text = (pdf_data['descripcion'] or "").lower()
                if text_filter not in desc_text:
                    # Opcional: imprimir rechazo por texto (puede llenar mucho la consola)
                    # print(f"RECHAZADO '{nombre_archivo}': Texto no encontrado")
                    continue

            # ¡SI LLEGA AQUÍ, ES QUE PASÓ TODO!
            self.filtered_list.append(pdf_data)

        print(f"RESULTADO: Pasaron {len(self.filtered_list)} de {len(self.pdf_list)} archivos.")
        print("-----------------------")
        
        # Actualizamos la tabla
        self.update_listbox()
            


    def update_listbox(self):
        for item in self.tree.get_children():
            self.tree.delete(item)
            
        for pdf_data in self.filtered_list:
            fecha_str = pdf_data['fecha_inicio'].strftime('%d.%m.%Y') if pdf_data['fecha_inicio'] else 'N/A'
            operador_str = ", ".join(pdf_data['operadores']) if pdf_data['operadores'] else 'N/A'
            desc_str = pdf_data['descripcion'][:40] if pdf_data['descripcion'] else 'Sin descripción'
            estado_str = "CERRADO PM SAP" if pdf_data['cerrado'] else "ABIERTO"

            self.tree.insert("","end",values=(
                desc_str,
                operador_str,
                fecha_str,
                estado_str,
                pdf_data['filename']
            ))

        self.result_label.config(text=f"Total: {len(self.filtered_list)} orden(es) encontrada(s)")
        


    def preview_pdf(self, event):
        print("DEBUG: ¡Clic detectado en el árbol!") 

        # 1. Obtener qué se seleccionó
        selection = self.tree.selection()

        # 2. Si no hay nada seleccionado, bloquear botones y salir
        if not selection:
            self.edit_button.config(state='disabled')
            self.obs_button.config(state='disabled')
            return

        # 3. CREAR LA VARIABLE (¡Esto debe ir primero!)
        item_id = selection[0]            
        index = self.tree.index(item_id) 
        pdf_data = self.filtered_list[index]  # <--- AQUÍ NACE LA VARIABLE
        
        # 4. USAR LA VARIABLE (Ahora sí podemos preguntar)
        
        # --- Lógica para botón de Observaciones ---
        if pdf_data.get('observaciones'):
            self.obs_button.config(state='normal')
        else:
            self.obs_button.config(state='disabled')

        # --- Lógica para botón de Editar ---
        if pdf_data['cerrado']:
            self.edit_button.config(state='disabled')
        else:
            self.edit_button.config(state='normal')

        # 5. Cargar la imagen
        full_path = os.path.join(self.carpeta_pdfs, pdf_data['filename'])
        try:
            doc = fitz.open(full_path)
            page = doc.load_page(0)
            pix = page.get_pixmap(matrix=fitz.Matrix(2, 2))
            img_data = pix.tobytes("png")
            image = Image.open(BytesIO(img_data))
            image.thumbnail((550, 950), Image.Resampling.LANCZOS)
            self.preview_image = ImageTk.PhotoImage(image)
            
            self.canvas.delete("all")
            self.canvas.create_image(280, 370, anchor='center', image=self.preview_image)
            doc.close()
        except Exception as e:
            self.canvas.delete("all")
            self.canvas.create_text(250, 250, 
                                    text=f"Error al cargar imagen:\n{e}", 
                                    fill="red", width=380, justify='center')
        
    def open_pdf(self, event):
        # --- LÓGICA NUEVA DE TREEVIEW ---
        selection = self.tree.selection()
        if not selection:
            return
        item_id = selection[0]
        index = self.tree.index(item_id)
        pdf_data = self.filtered_list[index]
        # -------------------------------
        
        full_path = os.path.join(self.carpeta_pdfs, pdf_data['filename'])

        try:
            os.startfile(full_path)
        except AttributeError:
            try:
                os.system(f'open "{full_path}"')
            except Exception:
                os.system(f'xdg-open "{full_path}"')
                

    def edit_pdf(self):
        selection = self.tree.selection() # <--- CAMBIO
        if not selection:
            return
        item_id = selection[0]            # <--- CAMBIO
        index = self.tree.index(item_id)  # <--- CAMBIO
        pdf_data = self.filtered_list[index]
        
        full_path = os.path.join(self.carpeta_pdfs, pdf_data['filename'])

        if pdf_data['cerrado']:
            messagebox.showwarning("Aviso", "Este PDF está CERRADO PM SAP y no puede editarse.")
            return

        csharp_exe = os.path.join(BASE_DIR, "EditorPDF", "WpfApp2.exe")
        try:
            subprocess.Popen([csharp_exe, full_path], shell=False)
        except FileNotFoundError:
            messagebox.showwarning(
                "Aviso",
                f"No se encontró el programa C#:\n{csharp_exe}\n\nVerifica la ruta del ejecutable."
            )
        except Exception as e:
            messagebox.showerror("Error", f"Error al abrir editor:\n{str(e)}")
    

if __name__ == "__main__":
    root = tk.Tk()
    app = PDFOrganizerApp(root)
    
    root.mainloop()

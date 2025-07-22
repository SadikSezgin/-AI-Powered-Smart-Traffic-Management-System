from flask import Flask, request, jsonify
from geohash2 import encode as geohash_encode
import joblib
import json

model_path = r"C:\Users\Sadik\Desktop\bitirme\Project\Model\catboost_model.pkl"
model = joblib.load(model_path)

with open("C:/Users/Sadik/Desktop/bitirme/Project/Model/location_mapping.json", "r") as f:
    location_mapping = json.load(f)

app = Flask(__name__)

@app.route('/predict', methods=['POST'])
def predict():
    data = request.get_json()
    print("[📥 RECEIVED PAYLOAD]", data)

    try:
        
        lat = data['latitude']
        lon = data['longitude']
        geohash = geohash_encode(lat, lon, precision=6)
        
        if geohash not in location_mapping:
            return jsonify({'error': f"Geohash '{geohash}' not found in mapping"}), 400
        
        location_encoded = location_mapping[geohash]

        input_data = [[
            data['hour'],
            data['dayOfWeek'],
            data['isWeekend'],
            location_encoded,
            data['numberOfVehicles']
        ]]
        prediction = model.predict(input_data)[0]
        return jsonify({'PredictedSpeed': float(prediction)})
    
    except Exception as e:
        return jsonify({'error': str(e)}), 400

if __name__ == '__main__':
    print(" CatBoost prediction API running on http://localhost:5006/predict")
    app.run(host='0.0.0.0', port=5006)
